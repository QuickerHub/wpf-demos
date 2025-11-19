namespace QuickerExpressionAgent.Common;

/// <summary>
/// Interface for handling expression agent tool calls
/// UI layer should implement this interface to handle tool execution and variable updates
/// This interface only handles data operations, formatting logic is in ExpressionAgentPlugin
/// </summary>
public interface IExpressionAgentToolHandler
{
    /// <summary>
    /// Update or set the current expression
    /// </summary>
    /// <param name="expression">The expression to set</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> UpdateExpressionAsync(string expression);
    
    /// <summary>
    /// Create a new variable
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="type">Variable type</param>
    /// <param name="defaultValue">Default value (optional)</param>
    /// <returns>True if successful, false if variable already exists</returns>
    Task<bool> CreateVariableAsync(string name, VariableType type, string? defaultValue = null);
    
    /// <summary>
    /// Update an existing variable's value
    /// </summary>
    /// <param name="variable">Variable information with updated value</param>
    /// <returns>True if successful, false if variable not found</returns>
    Task<bool> UpdateVariableAsync(VariableClass variable);
    
    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    /// <param name="expression">Expression to test (optional, uses current if null)</param>
    /// <returns>Expression execution result</returns>
    Task<ExpressionResult> TestExpressionAsync(string? expression = null);
    
    /// <summary>
    /// Get external variables (variables that are inputs to the expression)
    /// </summary>
    /// <returns>Current external variable list</returns>
    Task<List<VariableClass>> GetExternalVariablesAsync();
    
    /// <summary>
    /// Get current expression code
    /// </summary>
    /// <returns>Current expression code (C# code with {variableName} format)</returns>
    Task<string> GetExpressionAsync();
}

public interface IExpressionAgentToolHandler2
{
    string Expression { get; set; }
    void SetVariable(VariableClass variable);
    VariableClass GetVariable(string name);
    List<VariableClass> GetAllVariables();
}