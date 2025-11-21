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
            return new ExpressionResult
            {
                Success = false,
                Error = "Expression cannot be empty."
            };
        }
        
        try
        {
            // Use provided variables, or fall back to current variables
            List<VariableClass> variablesToUse = variables ?? GetAllVariables();
            
            // Execute expression using executor
            var result = await _executor.ExecuteExpressionAsync(expression, variablesToUse);
            
            return result;
        }
        catch (Exception ex)
        {
            return new ExpressionResult
            {
                Success = false,
                Error = $"Error testing expression: {ex.Message}"
            };
        }
    }
}

