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
    /// Description of the JSON format for variables
    /// </summary>
    private const string VariablesJsonFormatDescription = 
        "Array format (JSON array): [{\"VarName\":\"name\",\"VarType\":\"String\",\"DefaultValue\":\"value\"},...]. " +
        "Must be an array of objects. Each object in the array must have: VarName (string), VarType (String|Int|Double|Bool|DateTime|ListString|Dictionary|Object), DefaultValue (type depends on VarType). " +
        "DefaultValue type must match VarType: String uses string, Int uses number, Double uses number, Bool uses boolean, " +
        "DateTime uses ISO8601 string, ListString uses array of strings [\"item1\",\"item2\"], " +
        "Dictionary uses object {\"key\":\"value\"}, Object uses any JSON value. " +
        "Example array: [{\"VarName\":\"userName\",\"VarType\":\"String\",\"DefaultValue\":\"John\"},{\"VarName\":\"age\",\"VarType\":\"Int\",\"DefaultValue\":25}]";
    
    private readonly IExpressionAgentToolHandler _toolHandler;
    private readonly IRoslynExpressionService? _roslynService;

    public ExpressionAgentPlugin(IExpressionAgentToolHandler toolHandler, IRoslynExpressionService? roslynService = null)
    {
        _toolHandler = toolHandler;
        _roslynService = roslynService;
    }


    [KernelFunction]
    [Description("Get all external variables (variables that are inputs to the expression). These are variables that can be referenced in expressions using {variableName} format. Returns a formatted string with variable names and types only (no default values).")]
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

        // Update variable with new default value
        var variable = new VariableClass
        {
            VarName = name,
            VarType = existing.VarType,
            DefaultValue = defaultValue
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
        [Description("Expression to test (C# code with {variableName} format)")] string expression,
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
            List<VariableClass>? variablesToUse = null;
            
            // Parse variables if provided
            if (variables != null && variables.Count > 0)
            {
                // Get all existing variables from tool handler
                var existingVariables = _toolHandler.GetAllVariables();
                
                // Create list with updated default values from dictionary
                variablesToUse = new List<VariableClass>();
                
                foreach (var existingVar in existingVariables)
                {
                    var variableToUse = new VariableClass
                    {
                        VarName = existingVar.VarName,
                        VarType = existingVar.VarType,
                        DefaultValue = existingVar.DefaultValue // Use existing default value
                    };
                    
                    // Override default value if provided in dictionary
                    if (variables.TryGetValue(existingVar.VarName, out var newValue))
                    {
                        variableToUse.DefaultValue = newValue;
                    }
                    
                    variablesToUse.Add(variableToUse);
                }
                
                // Check for variables in dictionary that don't exist
                var missingVariables = variables.Keys
                    .Where(varName => !existingVariables.Any(v => 
                        string.Equals(v.VarName, varName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                if (missingVariables.Any())
                {
                    return $"Error: The following variables do not exist and must be created first using CreateVariable: {string.Join(", ", missingVariables)}";
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

    [KernelFunction]
    [Description("Set the final expression. This should be called only after the expression has been tested and verified to work correctly. This is the final step to output the completed expression. Use CreateVariable method to create or update variables separately.")]
    public string SetExpression(
        [Description("Final C# expression code with {variableName} format")] string expression)
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

