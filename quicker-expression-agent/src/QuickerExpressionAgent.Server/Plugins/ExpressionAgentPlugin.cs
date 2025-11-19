using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server.Plugins;

/// <summary>
/// Plugin for Expression Agent tools - provides tools for creating variables, modifying expressions, and testing
/// </summary>
public class ExpressionAgentPlugin
{
    /// <summary>
    /// Description of the expression format with {variableName} syntax for external variables
    /// </summary>
    private const string ExpressionFormatDescription = 
        "C# code with {variableName} format. " +
        "The expression is pure C# code that can be executed directly. " +
        "To reference external variables (input variables), use {variableName} format, e.g., {userName}, {age}, {items}. " +
        "During execution, {variableName} will be replaced with the actual variable name for parsing. " +
        "Example: \"Hello, \" + {userName} + \"!\" will become \"Hello, \" + userName + \"!\" after replacement.";
    
    private readonly IExpressionAgentToolHandler _toolHandler;
    private readonly IRoslynExpressionService? _roslynService;

    public ExpressionAgentPlugin(IExpressionAgentToolHandler toolHandler, IRoslynExpressionService? roslynService = null)
    {
        _toolHandler = toolHandler;
        _roslynService = roslynService;
    }


    [KernelFunction]
    [Description($"Get all external variables (variables that are inputs to the expression). These are variables that can be referenced in expressions using {{variableName}} format. Returns a formatted string with variable names and types only (no default values). Expression format: {ExpressionFormatDescription}")]
    public string GetExternalVariables()
    {
        var variables = _toolHandler.GetAllVariables();
        
        if (!variables.Any())
        {
            return "No external variables defined.";
        }
        
        var description = new StringBuilder();
        description.AppendLine($"External Variables ({variables.Count}):");
        foreach (var variable in variables)
        {
            description.AppendLine($"  - {variable.VarName} ({variable.VarType})");
        }
        
        return description.ToString();
    }

    [KernelFunction]
    [Description("Create or update a single variable. If a variable already exists, it will be updated. Variable types: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object.")]
    public string CreateVariable(
        [Description("Variable name")] string name,
        [Description("Variable type: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object")] VariableType varType,
        [Description("Default value (can be any type matching the variable type)")] object? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Error: Variable name cannot be empty.";
        }

        try
        {
            // Check if variable already exists
            var existing = _toolHandler.GetVariable(name);
            bool isNew = existing == null;
            
            // Create VariableClass and set variable
            var variable = new VariableClass
            {
                VarName = name,
                VarType = varType,
                DefaultValue = defaultValue
            };
            
            _toolHandler.SetVariable(variable);
            
            if (isNew)
            {
                return $"Variable '{name}' ({varType}) created successfully.";
            }
            else
            {
                return $"Variable '{name}' ({varType}) updated successfully.";
            }
        }
        catch (Exception ex)
        {
            return $"Error processing variable '{name}': {ex.Message}";
        }
    }
    
    [KernelFunction]
    [Description("Get a specific variable's information including its default value. Use this when you need to see the actual default value of a variable, especially when processing format strings or when you need to understand the variable's content. Returns the variable information with full default value.")]
    public VariableClass? GetVariable(
    [Description("Name of the variable to retrieve")] string variableName)
    {
        return _toolHandler.GetVariable(variableName);
    }
    
    [KernelFunction]
    [Description("Set the default value of an existing external variable. The default value can be of any type: string, number (int/double), boolean, DateTime, array (List<string>), object (Dictionary), or null.")]
    public string SetVarDefaultValue(
        [Description("Variable name")] string name,
        [Description("Default value (can be any type matching the variable type)")] object? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Error: Variable name cannot be empty.";
        }

        // Get existing variable to preserve its type
        var existing = _toolHandler.GetVariable(name);
        if (existing == null)
        {
            return $"Error: Variable '{name}' not found. Use CreateVariable to create it first.";
        }

        // Convert defaultValue to correct type, handling JsonElement if present
        var convertedValue = ConvertValueToVariableType(defaultValue, existing.VarType);

        // Update variable with new default value
        var variable = new VariableClass
        {
            VarName = name,
            VarType = existing.VarType,
            DefaultValue = convertedValue
        };
        
        _toolHandler.SetVariable(variable);
        return $"Variable '{name}' default value set successfully.";
    }

    [KernelFunction]
    [Description("Get the current expression and variables as a formatted string description. This provides a human-readable summary of the current expression code and all external variables with their types (without default values). Use this when you need to understand the current state before modifying the expression. To get a specific variable's default value, use GetVariable method.")]
    public string GetCurrentExpressionDescription()
    {
        var expression = _toolHandler.Expression;
        var variables = _toolHandler.GetAllVariables();

        if (string.IsNullOrWhiteSpace(expression) && !variables.Any())
        {
            return "No expression or variables currently set.";
        }

        var description = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(expression))
        {
            description.AppendLine("Current Expression:");
            description.AppendLine("```csharp");
            description.AppendLine(expression);
            description.AppendLine("```");
        }
        else
        {
            description.AppendLine("Current Expression: (empty)");
        }

        description.AppendLine();

        if (variables.Any())
        {
            description.AppendLine($"External Variables ({variables.Count}):");
            foreach (var variable in variables)
            {
                description.AppendLine($"  - {variable.VarName} ({variable.VarType})");
            }
        }
        else
        {
            description.AppendLine("External Variables: (none)");
        }

        return description.ToString();
    }

    [KernelFunction]
    [Description("Test an expression with optional variable default values. This allows testing expressions with different variable values without modifying the UI variables. The variables must already exist (created via CreateVariable). If variables parameter is not provided, uses the current UI variable default values.")]
    public async Task<string> TestExpression(
        [Description($"Expression to test. {ExpressionFormatDescription}")] string expression,
        [Description("Optional dictionary of variable names to default values. Format: {\"variableName\": value, ...}. Variables must already exist (created via CreateVariable). The dictionary allows setting temporary default values for testing without modifying UI variables. Example: {\"userName\": \"John\", \"age\": 25}")] Dictionary<string, object>? variables = null)
    {
        if (_roslynService == null)
        {
            return "Error: Roslyn service not available. Cannot test expression.";
        }

        if (string.IsNullOrWhiteSpace(expression))
        {
            return "Error: Expression cannot be empty.";
        }

        try
        {
            // Get all existing variables from tool handler
            var existingVariables = _toolHandler.GetAllVariables();
            
            // Build variable list: start with all external variables, then override with dictionary values if provided
            var variablesToUse = existingVariables.Select(v => new VariableClass
            {
                VarName = v.VarName,
                VarType = v.VarType,
                DefaultValue = v.DefaultValue
            }).ToList();
            
            // Override default values with dictionary values if provided
            if (variables != null && variables.Count > 0)
            {
                // Check if all variables in dictionary exist
                var missingVariables = variables.Keys
                    .Where(varName => !existingVariables.Any(v => 
                        string.Equals(v.VarName, varName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (missingVariables.Any())
                {
                    return $"Error: The following variables do not exist and must be created first using CreateVariable: {string.Join(", ", missingVariables)}";
                }
                
                // Override default values with dictionary values
                foreach (var kvp in variables)
                {
                    var varName = kvp.Key;
                    var value = kvp.Value;
                    
                    var variableToUpdate = variablesToUse.FirstOrDefault(v => 
                        string.Equals(v.VarName, varName, StringComparison.OrdinalIgnoreCase));
                    
                    if (variableToUpdate != null)
                    {
                        // Convert value to correct type, handling JsonElement if present
                        variableToUpdate.DefaultValue = ConvertValueToVariableType(value, variableToUpdate.VarType);
                    }
                }
            }
            
            // Use tool handler's TestExpression
            var result = await _toolHandler.TestExpression(expression, variablesToUse);

            if (result.Success)
            {
                var resultJson = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { WriteIndented = true });
                return $"Expression executed successfully. Result: {resultJson}";
            }
            else
            {
                // Check if error indicates missing variables
                var errorMessage = result.Error ?? "Unknown error";
                if (errorMessage.Contains("does not exist") || (errorMessage.Contains("The name") && errorMessage.Contains("does not exist in the current context")))
                {
                    // Try to extract variable name from error message
                    var missingVarMatch = Regex.Match(errorMessage, @"(?:The name '|')([a-zA-Z_][a-zA-Z0-9_]*)'? does not exist");
                    if (missingVarMatch.Success && missingVarMatch.Groups.Count > 1)
                    {
                        var missingVar = missingVarMatch.Groups[1].Value;
                        return $"Error: Missing variable '{missingVar}' required by the expression. Please create this variable first using CreateVariable method. Full error: {errorMessage}";
                    }
                }
                
                return $"Expression execution failed. Error: {errorMessage}";
            }
        }
        catch (Exception ex)
        {
                return $"Error testing expression: {ex.Message}";
        }
    }

    /// <summary>
    /// Convert a value to the correct type for the given VariableType
    /// Handles JsonElement conversion if the value is a JsonElement
    /// </summary>
    private object ConvertValueToVariableType(object value, VariableType varType)
    {
        // If value is JsonElement, use ConvertValueFromJson
        if (value is JsonElement jsonElement)
        {
            return varType.ConvertValueFromJson(jsonElement);
        }
        
        // If value is already the correct type, return as-is
        var valueType = value?.GetType();
        if (valueType != null)
        {
            // Check if the value type matches the expected type
            var expectedType = GetExpectedType(varType);
            if (expectedType != null && expectedType.IsAssignableFrom(valueType))
            {
                return value;
            }
        }
        
        // If value is null, return default for the type
        if (value == null)
        {
            return varType.GetDefaultValue();
        }
        
        // Try to convert using string representation
        try
        {
            return varType.ConvertValueFromString(value.ToString());
        }
        catch
        {
            // If conversion fails, return default for the type
            return varType.GetDefaultValue();
        }
    }
    
    /// <summary>
    /// Get the expected .NET type for a VariableType
    /// </summary>
    private Type? GetExpectedType(VariableType varType)
    {
        return varType switch
        {
            VariableType.String => typeof(string),
            VariableType.Int => typeof(int),
            VariableType.Double => typeof(double),
            VariableType.Bool => typeof(bool),
            VariableType.DateTime => typeof(DateTime),
            VariableType.ListString => typeof(List<string>),
            VariableType.Dictionary => typeof(Dictionary<string, object>),
            VariableType.Object => typeof(object),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// Extract variable names from expression (format: {varname})
    /// </summary>
    private List<string> ExtractVariableNamesFromExpression(string expression)
    {
        var variableNames = new List<string>();
        
        if (string.IsNullOrWhiteSpace(expression))
        {
            return variableNames;
        }
        
        // Match {varname} pattern
        var pattern = @"\{([a-zA-Z_][a-zA-Z0-9_]*)\}";
        var matches = Regex.Matches(expression, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var varName = match.Groups[1].Value;
                if (!variableNames.Contains(varName, StringComparer.OrdinalIgnoreCase))
                {
                    variableNames.Add(varName);
                }
            }
        }
        
        return variableNames;
    }

    [KernelFunction]
    [Description("Set the final expression. This should be called only after the expression has been tested and verified to work correctly. This is the final step to output the completed expression. Use CreateVariable method to create or update variables separately.")]
    public string SetExpression(
        [Description($"Final expression code. {ExpressionFormatDescription}")] string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return "Error: Expression cannot be empty.";
        }

        try
        {
            // Update expression
            _toolHandler.Expression = expression;
            return "Expression set successfully.";
        }
        catch (Exception ex)
        {
            return $"Error setting expression: {ex.Message}";
        }
    }

}

