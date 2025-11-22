using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickerExpressionAgent.Common;
using Z.Expressions;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Helper class for testing expressions with compile and execution
/// </summary>
internal static class ExpressionTestHelper
{
    /// <summary>
    /// Result of expression processing (placeholder replacement and parameter extraction)
    /// </summary>
    public class ProcessedExpression
    {
        public string Code { get; set; } = string.Empty;
        public List<VariableClass> UsedVariables { get; set; } = new();
        public Dictionary<string, object?> Parameters { get; set; } = new();
        public Dictionary<string, Type> ParameterTypes { get; set; } = new();
    }

    /// <summary>
    /// Process expression: replace placeholders and extract parameters
    /// </summary>
    public static ProcessedExpression ProcessExpression(string expression, List<VariableClass> variables)
    {
        var result = new ProcessedExpression
        {
            Code = expression,
            UsedVariables = new List<VariableClass>(),
            Parameters = new Dictionary<string, object?>(StringComparer.Ordinal),
            ParameterTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
        };

        foreach (var variable in variables)
        {
            var placeholder = $"{{{variable.VarName}}}";
            if (result.Code.Contains(placeholder, StringComparison.Ordinal))
            {
                result.UsedVariables.Add(variable);
                
                // Get default value
                var defaultValue = variable.GetDefaultValue();
                result.Parameters[variable.VarName] = defaultValue;
                
                // Get the actual type of the default value
                var valueType = defaultValue?.GetType() ?? variable.VarType.GetDefaultValue().GetType();
                result.ParameterTypes[variable.VarName] = valueType;
                
                // Replace placeholder with variable name
                result.Code = result.Code.Replace(placeholder, variable.VarName);
            }
        }

        return result;
    }

    /// <summary>
    /// Test expression with compile and execution
    /// </summary>
    public static ExpressionResult TestExpression(
        EvalContext evalContext,
        string expression,
        List<VariableClass> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new ExpressionResultError("Expression cannot be null or empty");
        }

        try
        {
            // Process expression: replace placeholders and extract parameters
            var processed = ProcessExpression(expression, variables);

            // Step 1: Compile expression to check syntax
            Func<IDictionary, object?> compiledExpression;
            try
            {
                compiledExpression = evalContext.Compile(processed.Code, processed.ParameterTypes);
            }
            catch (Exception ex)
            {
                return new ExpressionResultError($"Syntax error: {ex.Message}");
            }

            // Step 2: Execute compiled expression
            object? result;
            try
            {
                // Convert Dictionary<string, object?> to IDictionary (non-generic)
                IDictionary parametersDict = processed.Parameters;
                result = compiledExpression(parametersDict);
            }
            catch (Exception ex)
            {
                return new ExpressionResultError($"Execution error: {ex.Message}");
            }

            return new ExpressionResult(result, processed.UsedVariables);
        }
        catch (Exception ex)
        {
            return new ExpressionResultError(ex.Message);
        }
    }
}

