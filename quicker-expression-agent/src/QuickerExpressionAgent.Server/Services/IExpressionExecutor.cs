using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Service interface for executing C# expressions using Roslyn
/// </summary>
public interface IExpressionExecutor
{
    /// <summary>
    /// Execute a C# expression using Roslyn scripting
    /// Variables are provided via dictionary and are directly accessible in the script by name
    /// </summary>
    /// <param name="code">Expression code. Variables are directly accessible by name (no {varname} format needed)</param>
    /// <param name="variables">Optional dictionary of variable names to values. Variables are directly accessible in the script.</param>
    /// <returns>Expression execution result</returns>
    Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        Dictionary<string, object>? variables = null);
    
    /// <summary>
    /// Execute a C# expression using Roslyn scripting (legacy method for backward compatibility)
    /// </summary>
    /// <param name="code">Expression code (can contain {varname} format for variable references)</param>
    /// <param name="variableList">List of variable declarations</param>
    /// <returns>Expression execution result</returns>
    Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        List<VariableClass> variableList);
}

