namespace QuickerExpressionAgent.Common;

/// <summary>
/// Interface for handling expression agent tool calls
/// UI layer should implement this interface to handle tool execution and variable updates
/// This interface only handles data operations, formatting logic is in ExpressionAgentPlugin
/// </summary>
public interface IExpressionAgentToolHandler
{
    /// <summary>
    /// Current expression code (C# code with {variableName} format)
    /// </summary>
    string Expression { get; set; }
    
    /// <summary>
    /// Set or update a variable
    /// </summary>
    /// <param name="variable">Variable information</param>
    void SetVariable(VariableClass variable);
    
    /// <summary>
    /// Get a specific variable by name
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <returns>Variable information, or null if not found</returns>
    VariableClass? GetVariable(string name);
    
    /// <summary>
    /// Get all variables
    /// </summary>
    /// <returns>List of all variables</returns>
    List<VariableClass> GetAllVariables();
    
    /// <summary>
    /// Test an expression for syntax and execution
    /// </summary>
    /// <param name="expression">Expression to test</param>
    /// <param name="variables">Optional list of variables with default values (uses current variables if null)</param>
    /// <returns>Expression execution result</returns>
    Task<ExpressionResult> TestExpression(string expression, List<VariableClass>? variables = null);
}