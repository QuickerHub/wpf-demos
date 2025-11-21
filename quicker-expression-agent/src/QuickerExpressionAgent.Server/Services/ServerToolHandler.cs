using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Server tool handler implementation with variable list
/// </summary>
public class ServerToolHandler : IExpressionAgentToolHandler
{
    private readonly IExpressionExecutor _executor;
    private readonly List<VariableClass> _variables;
    private readonly object _variablesLock = new object();
    
    public ServerToolHandler(IExpressionExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _variables = new List<VariableClass>();
    }
    
    public string Expression { get; set; } = string.Empty;
    
    public void SetVariable(VariableClass variable)
    {
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }
        
        lock (_variablesLock)
        {
            var existingIndex = _variables.FindIndex(v => v.VarName == variable.VarName);
            if (existingIndex >= 0)
            {
                // Update existing variable
                _variables[existingIndex] = variable;
            }
            else
            {
                // Add new variable
                _variables.Add(variable);
            }
        }
    }
    
    public VariableClass? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        
        lock (_variablesLock)
        {
            return _variables.FirstOrDefault(v => v.VarName == name);
        }
    }
    
    public List<VariableClass> GetAllVariables()
    {
        lock (_variablesLock)
        {
            // Return a copy to prevent external modification
            return new List<VariableClass>(_variables);
        }
    }
    
    public async Task<ExpressionResult> TestExpressionAsync(string expression, List<VariableClass>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return new ExpressionResultError("Expression cannot be empty.");
        }
        
        try
        {
            // Merge variables: GetAllVariables() first, then override/add with provided variables
            // Use dictionary merge similar to CodeEditorExpressionToolHandler to include new variables
            var variablesDict = GetAllVariables().ToDictionary(
                v => v.VarName, 
                v => new VariableClass
                {
                    VarName = v.VarName,
                    VarType = v.VarType,
                    DefaultValue = v.DefaultValue // Already a string, just copy
                }, 
                StringComparer.Ordinal);
            
            // Add or override with provided variables
            if (variables != null && variables.Count > 0)
            {
                foreach (var variable in variables)
                {
                    // Create a copy of the variable with proper default value handling
                    var variableToAdd = new VariableClass
                    {
                        VarName = variable.VarName,
                        VarType = variable.VarType
                    };
                    
                    // Get the default value and set it (handles both string and JsonElement cases)
                    var defaultValue = variable.GetDefaultValue();
                    variableToAdd.SetDefaultValue(defaultValue);
                    
                    // Add or update in dictionary (case-sensitive match)
                    variablesDict[variable.VarName] = variableToAdd;
                }
            }
            
            // Convert dictionary values to list for executor
            var variablesToUse = variablesDict.Values.ToList();
            
            // Execute expression using executor
            var result = await _executor.ExecuteExpressionAsync(expression, variablesToUse);
            
            return result;
        }
        catch (Exception ex)
        {
            return new ExpressionResultError($"Error testing expression: {ex.Message}");
        }
    }
}

