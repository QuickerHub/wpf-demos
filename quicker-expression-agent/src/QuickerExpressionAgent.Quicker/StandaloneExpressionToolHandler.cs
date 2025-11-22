using System;
using System.Collections;
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
    public Task<ExpressionResult> TestExpressionAsync(string expression, List<VariableClass>? variables = null)
        {
            // Use provided variables or current variables
            var varsToUse = variables ?? GetAllVariables();

            // Create a temporary EvalContext for testing
            var testEvalContext = new EvalContext();

        // Register variables in test context (for any potential side effects)
            foreach (var variable in varsToUse)
            {
                var value = ConvertVariableToValue(variable);
                testEvalContext.RegisterLocalVariable(variable.VarName, value);
            }

        // Use helper method to test expression
        var result = ExpressionTestHelper.TestExpression(testEvalContext, expression, varsToUse);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Convert VariableClass to actual value based on variable type
    /// GetDefaultValue() already returns the correct type, just handle null case
    /// </summary>
    /// <param name="variable">Variable information</param>
    /// <returns>Converted value</returns>
    private object ConvertVariableToValue(VariableClass variable)
    {
        // GetDefaultValue() already handles deserialization and returns correct type
        var value = variable.GetDefaultValue();
        // Return default value for type if null
        return value ?? variable.VarType.GetDefaultValue();
    }
}

