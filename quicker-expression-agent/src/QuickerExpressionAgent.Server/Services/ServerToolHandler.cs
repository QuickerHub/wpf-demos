using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Server tool handler implementation with variable list
/// </summary>
public class ServerToolHandler : IExpressionAgentToolHandler
{
    private readonly IRoslynExpressionService _roslynService;
    private readonly List<VariableClass> _variables;
    private readonly object _variablesLock = new object();
    
    public ServerToolHandler(IRoslynExpressionService roslynService)
    {
        _roslynService = roslynService ?? throw new ArgumentNullException(nameof(roslynService));
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
    
    public async Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null)
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
            
            // Execute expression using Roslyn service
            var result = await _roslynService.ExecuteExpressionAsync(expression, variablesToUse);
            
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

