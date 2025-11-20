using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuickerExpressionAgent.Common;
using Z.Expressions;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Standalone expression tool handler that executes expressions directly using EvalContext
/// This implementation does not depend on CodeEditor window handles
/// </summary>
public class StandaloneExpressionToolHandler : IExpressionAgentToolHandler
{
    private readonly EvalContext _evalContext;
    private readonly Dictionary<string, VariableClass> _variables = new();

    /// <summary>
    /// Constructor - creates a new EvalContext instance
    /// </summary>
    public StandaloneExpressionToolHandler()
    {
        _evalContext = new EvalContext();
    }

    /// <summary>
    /// Current expression code (C# code with {variableName} format)
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Set or update a variable
    /// </summary>
    /// <param name="variable">Variable information</param>
    public void SetVariable(VariableClass variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (string.IsNullOrWhiteSpace(variable.VarName))
        {
            throw new ArgumentException("Variable name cannot be null or empty", nameof(variable));
        }

        _variables[variable.VarName] = variable;

        // Register variable in EvalContext
        // Convert VariableClass to actual value based on type
        var value = ConvertVariableToValue(variable);
        _evalContext.RegisterLocalVariable(variable.VarName, value);
    }

    /// <summary>
    /// Get a specific variable by name
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <returns>Variable information, or null if not found</returns>
    public VariableClass? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _variables.TryGetValue(name, out var variable) ? variable : null;
    }

    /// <summary>
    /// Get all variables
    /// </summary>
    /// <returns>List of all variables</returns>
    public List<VariableClass> GetAllVariables()
    {
        return _variables.Values.ToList();
    }

    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    /// <param name="expression">Expression to test</param>
    /// <param name="variables">Optional list of variables with default values (uses current variables if null)</param>
    /// <returns>Expression execution result</returns>
    public Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Task.FromResult(new ExpressionResult
            {
                Success = false,
                Error = "Expression cannot be null or empty"
            });
        }

        try
        {
            // Use provided variables or current variables
            var varsToUse = variables ?? GetAllVariables();

            // Create a temporary EvalContext for testing
            var testEvalContext = new EvalContext();

            // Register variables in test context
            foreach (var variable in varsToUse)
            {
                var value = ConvertVariableToValue(variable);
                testEvalContext.RegisterLocalVariable(variable.VarName, value);
            }

            // Replace variable placeholders {variableName} with actual variable names
            var code = expression;
            foreach (var variable in varsToUse)
            {
                code = code.Replace($"{{{variable.VarName}}}", variable.VarName);
            }

            // Execute expression
            // TODO: EvalContext.Execute requires IActionContext and CustomData
            // Need to determine how to handle this in standalone mode
            // For now, using placeholder - actual implementation depends on EvalContext API
            object? result = null;
            try
            {
                // Placeholder: eval.Execute(code, customData)
                // Need to check EvalContext API for standalone execution without IActionContext
                // Possible alternatives:
                // - eval.Compile(code).Invoke()
                // - eval.Execute(code) if it supports null/empty CustomData
                // - Create a minimal IActionContext wrapper
                
                // For now, this is a placeholder that needs to be implemented based on actual EvalContext API
                result = ExecuteExpressionInternal(testEvalContext, code);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ExpressionResult
                {
                    Success = false,
                    Error = ex.Message,
                    Value = null
                });
            }

            // Extract used variables from expression
            var usedVariables = ExtractUsedVariables(expression, varsToUse);

            return Task.FromResult(new ExpressionResult
            {
                Success = true,
                Value = result,
                Error = string.Empty,
                UsedVariables = usedVariables
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExpressionResult
            {
                Success = false,
                Error = ex.Message,
                Value = null
            });
        }
    }

    /// <summary>
    /// Execute expression using EvalContext
    /// </summary>
    /// <param name="evalContext">EvalContext instance</param>
    /// <param name="code">Expression code to execute</param>
    /// <returns>Execution result</returns>
    private object? ExecuteExpressionInternal(EvalContext evalContext, string code)
    {
        // Register _eval variable for self-reference (similar to ExpressionRunner)
        evalContext.RegisterLocalVariable("_eval", evalContext);
        
        try
        {
            // Execute expression with null CustomData (standalone mode without IActionContext)
            // CustomData is typically a Dictionary<string, object> used for custom data in Quicker context
            // In standalone mode, we pass null or empty dictionary
            var result = evalContext.Execute(code, null);
            return result;
        }
        finally
        {
            // Unregister _eval variable
            evalContext.UnregisterLocalVariable("_eval");
        }
    }

    /// <summary>
    /// Convert VariableClass to actual value based on variable type
    /// </summary>
    /// <param name="variable">Variable information</param>
    /// <returns>Converted value</returns>
    private object ConvertVariableToValue(VariableClass variable)
    {
        return variable.VarType switch
        {
            VariableType.String => variable.DefaultValue?.ToString() ?? string.Empty,
            VariableType.Int => Convert.ToInt32(variable.DefaultValue ?? 0),
            VariableType.Double => Convert.ToDouble(variable.DefaultValue ?? 0.0),
            VariableType.Bool => Convert.ToBoolean(variable.DefaultValue ?? false),
            VariableType.DateTime => variable.DefaultValue is DateTime dt ? dt : DateTime.Now,
            VariableType.ListString => variable.DefaultValue ?? new List<string>(),
            VariableType.Dictionary => variable.DefaultValue ?? new Dictionary<string, object>(),
            VariableType.Object => variable.DefaultValue ?? new object(),
            _ => variable.DefaultValue ?? new object()
        };
    }

    /// <summary>
    /// Extract variables that are actually used in the expression
    /// </summary>
    /// <param name="expression">Expression code</param>
    /// <param name="allVariables">All available variables</param>
    /// <returns>List of variables used in the expression</returns>
    private List<VariableClass> ExtractUsedVariables(string expression, List<VariableClass> allVariables)
    {
        var usedVariables = new List<VariableClass>();

        foreach (var variable in allVariables)
        {
            // Check if variable name appears in expression (after placeholder replacement)
            if (expression.Contains(variable.VarName, StringComparison.OrdinalIgnoreCase))
            {
                usedVariables.Add(variable);
            }
        }

        return usedVariables;
    }
}

