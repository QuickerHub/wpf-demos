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
/// Variable class with object DefaultValue for AI agent input
/// This allows AI to pass object values directly without JSON string conversion
/// </summary>
public class VariableClassWithObjectValue
{
    public string VarName { get; set; } = string.Empty;
    public VariableType VarType { get; set; } = VariableType.String;
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// Convert to VariableClass with string DefaultValue
    /// </summary>
    public VariableClass ToVariableClass()
    {
        var variable = new VariableClass
        {
            VarName = VarName,
            VarType = VarType
        };
        
        // VariableClass.SetDefaultValue will handle type conversion and serialization
        variable.SetDefaultValue(DefaultValue);
        
        return variable;
    }
}

/// <summary>
/// Plugin for Expression Agent tools - provides tools for creating variables, modifying expressions, and testing
/// </summary>
public class ExpressionAgentPlugin
{
    /// <summary>
    /// Description of the expression format with {variableName} syntax for external variables
    /// Think of {variableName} as function parameters (inputs) and the expression as the function body.
    /// </summary>
    private const string ExpressionFormatDescription = 
        "C# code with {variableName} format. " +
        "Think of it like a function: {variableName} is like function parameters (inputs), and the expression is the function body. " +
        "The expression is pure C# code that can be executed directly and computes a result (like a function return value) or performs an action (void function). " +
        "To reference external variables (input variables), use {variableName} format, e.g., {userName}, {age}, {items}. " +
        "During execution, {variableName} will be replaced with the actual variable name for parsing. " +
        "Example: \"Hello, \" + {userName} + \"!\" will become \"Hello, \" + userName + \"!\" after replacement. " +
        "**CRITICAL: {variableName} is like a function parameter - you CANNOT assign to it directly. For example, {varname} = value is NOT allowed and will NOT work.** " +
        "**Exception: For reference types (Dictionary, List, Object), you CAN modify properties/members, e.g., {dict}[\"key\"] = value or {list}.Add(item).** " +
        "**The expression should compute and return a result directly, NOT assign to variables. Example CORRECT: {inputDict}.Where(...).ToDictionary(...). Example WRONG: {outputDict} = {inputDict}.Where(...).ToDictionary(...).**";
    
    /// <summary>
    /// Description of the JSON format for List&lt;VariableClass&gt;
    /// </summary>
    private const string VariableListJsonFormatDescription = 
        "JSON array format: [{\"VarName\":\"string\",\"VarType\":\"String|Int|Double|Bool|DateTime|ListString|Dictionary|Object\",\"DefaultValue\":object value},...]. " +
        "Each object must have: VarName (string), VarType (String|Int|Double|Bool|DateTime|ListString|Dictionary|Object), DefaultValue (type depends on VarType). " +
        "DefaultValue type must match VarType: String uses string, Int uses number, Double uses number, Bool uses boolean, " +
        "DateTime uses ISO8601 string, ListString uses array of strings [\"item1\",\"item2\"], " +
        "Dictionary uses object {\"key\":\"value\"}, Object uses any JSON value. " +
        "Example: [{\"VarName\":\"userName\",\"VarType\":\"String\",\"DefaultValue\":\"John\"},{\"VarName\":\"age\",\"VarType\":\"Int\",\"DefaultValue\":25}]";
    
    /// <summary>
    /// Description of the JSON format for List&lt;VariableClassWithObjectValue&gt; (for TestExpression)
    /// </summary>
    private const string VariableClassWithObjectValueFormatDescription = 
        "JSON array format: [{\"VarName\":\"string\",\"VarType\":\"String|Int|Double|Bool|DateTime|ListString|Dictionary|Object\",\"DefaultValue\":value},...]. " +
        "DefaultValue can be any JSON value matching the VarType (string, number, boolean, array, object). " +
        "Variables must already exist (created via CreateVariable). This allows setting temporary default values for testing without modifying UI variables.";
    
    /// <summary>
    /// Description of variable naming convention
    /// </summary>
    private const string VariableNamingConventionDescription = 
        "Use concise, short names (e.g., text, list, dict, num, flag, date). Use numbered suffixes for multiple variables of the same type (e.g., text1, text2, list1, list2).";
    
    private readonly IToolHandlerProvider _toolHandlerProvider;

    public ExpressionAgentPlugin(IToolHandlerProvider toolHandlerProvider)
    {
        _toolHandlerProvider = toolHandlerProvider ?? throw new ArgumentNullException(nameof(toolHandlerProvider));
    }
    
    /// <summary>
    /// Gets the current tool handler from the provider
    /// </summary>
    private IExpressionAgentToolHandler ToolHandler => _toolHandlerProvider.ToolHandler;


    [KernelFunction]
    [Description($"Get all external variables (variables that are inputs to the expression). These are variables that can be referenced in expressions using {{variableName}} format. {ExpressionFormatDescription} Returns a formatted string with variable names and types only (no default values).")]
    public string GetExternalVariables()
    {
        var variables = ToolHandler.GetAllVariables();
        
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
    [Description("Create or update a single variable. If a variable already exists, it will be updated. Variable types: String, Int, Double, Bool, DateTime, ListString, Dictionary, Object. " + VariableNamingConventionDescription)]
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
            var existing = ToolHandler.GetVariable(name);
            bool isNew = existing == null;
            
            // Convert defaultValue to correct type, handling JsonElement if present
            var convertedValue = ConvertValueToVariableType(defaultValue, varType);
            
            // Create VariableClass and set variable
            var variable = new VariableClass
            {
                VarName = name,
                VarType = varType
            };
            // Serialize object to string (after conversion)
            variable.SetDefaultValue(convertedValue);
            
            ToolHandler.SetVariable(variable);
            
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
        return ToolHandler.GetVariable(variableName);
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
        var existing = ToolHandler.GetVariable(name);
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
            VarType = existing.VarType
        };
        // Serialize object to string
        variable.SetDefaultValue(convertedValue);
        
        ToolHandler.SetVariable(variable);
        return $"Variable '{name}' default value set successfully.";
    }

    [KernelFunction]
    [Description("Get the current expression and variables as a formatted string description. This provides a human-readable summary of the current expression code and all external variables with their types (without default values). Use this when you need to understand the current state before modifying the expression. To get a specific variable's default value, use GetVariable method.")]
    public string GetCurrentExpressionDescription()
    {
        var expression = ToolHandler.Expression;
        var variables = ToolHandler.GetAllVariables();

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
    [Description($"Test an expression with optional variable default values. {ExpressionFormatDescription} The variables must already exist (created via CreateVariable). If variables parameter is not provided, uses the current UI variable default values.")]
    public async Task<string> TestExpressionAsync(
        [Description($"Expression to test. {ExpressionFormatDescription}")] string expression,
        [Description($"Optional list of variables with default values. {VariableClassWithObjectValueFormatDescription}")] List<object>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return "Error: Expression cannot be empty.";
        }

        try
        {
            // Convert List<object> to List<VariableClass>
            // Serialize to JSON and deserialize directly to VariableClassWithObjectValue, then convert
            List<VariableClass>? convertedVariables = null;
            if (variables != null && variables.Count > 0)
            {
                try
                {
                    // Serialize List<object> to JSON string
                    var jsonString = JsonSerializer.Serialize(variables);
                    
                    // Deserialize directly to List<VariableClassWithObjectValue>
                    var variableList = JsonSerializer.Deserialize<List<VariableClassWithObjectValue>>(jsonString);
                    
                    if (variableList == null)
                    {
                        return "Error: Failed to deserialize variables from JSON.";
                    }
                    
                    // Convert VariableClassWithObjectValue to VariableClass
                    convertedVariables = variableList.Select(v => v.ToVariableClass()).ToList();
                }
                catch (JsonException ex)
                {
                    return $"Error: Invalid JSON format for variables parameter. {ex.Message}";
                }
                catch (Exception ex)
                {
                    return $"Error: Failed to parse variables parameter. {ex.Message}";
                }
            }
            
            // Use tool handler's TestExpression - it will handle variable merging internally
            var result = await ToolHandler.TestExpressionAsync(expression, convertedVariables);

            // Defensive check: ensure result is not null
            if (result == null)
            {
                return "Error: TestExpressionAsync returned null result.";
            }

            var response = new StringBuilder();
            
            if (result.Success)
            {
                var resultJson = string.IsNullOrWhiteSpace(result.ValueJson) ? "null" : result.ValueJson;
                var valueType = string.IsNullOrWhiteSpace(result.ValueType) ? "" : $" (Type: {result.ValueType})";
                response.AppendLine($"Success: {resultJson}{valueType}");
            }
            else
            {
                var errorMessage = result.Error ?? "Unknown error";
                response.AppendLine($"Error: {errorMessage}");
            }
            
            // Add used variables information (name and type only)
            if (result.UsedVariables != null && result.UsedVariables.Count > 0)
            {
                response.AppendLine($"Used Variables ({result.UsedVariables.Count}):");
                foreach (var usedVar in result.UsedVariables)
                {
                    response.AppendLine($"  - {usedVar.VarName} ({usedVar.VarType})");
                }
            }
            
            var responseText = response.ToString();
            
            // Ensure we always return something - this should never happen, but just in case
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return $"Error: Empty response from TestExpression. Success={result.Success}, Error={result.Error ?? "null"}, ValueJson={result.ValueJson ?? "null"}, ValueType={result.ValueType ?? "null"}";
            }
            
            return responseText;
        }
        catch (Exception ex)
        {
            // Include stack trace for debugging
            return $"Error: {ex.Message}\nStack trace: {ex.StackTrace}";
        }
    }

    /// <summary>
    /// Convert a value to the correct type for the given VariableType
    /// Handles JsonElement conversion if the value is a JsonElement
    /// </summary>
    private object? ConvertValueToVariableType(object? value, VariableType varType)
    {
        // Handle null value
        if (value == null)
        {
            return null;
        }
        
        // If value is JsonElement, use ConvertValueFromJson
        if (value is JsonElement jsonElement)
        {
            return varType.ConvertValueFromJson(jsonElement);
        }
        
        // If value is already the correct type, return as-is
        var valueType = value.GetType();
        var expectedType = GetExpectedType(varType);
        if (expectedType != null && expectedType.IsAssignableFrom(valueType))
        {
            return value;
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
    [Description($"Set the final expression. {ExpressionFormatDescription} This should be called only after the expression has been tested and verified to work correctly. This is the final step to output the completed expression. Use CreateVariable method to create or update variables separately.")]
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
            ToolHandler.Expression = expression;
            return "Expression set successfully.";
        }
        catch (Exception ex)
        {
            return $"Error setting expression: {ex.Message}";
        }
    }

}

