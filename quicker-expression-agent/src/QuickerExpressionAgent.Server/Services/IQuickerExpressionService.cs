using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Service interface for executing expressions in Quicker
/// </summary>
public interface IQuickerExpressionService
{
    /// <summary>
    /// Execute a C# expression in Quicker
    /// Expression uses {varname} format, which will be replaced with actual variable names during execution
    /// </summary>
    /// <param name="code">Expression code with {varname} format</param>
    /// <param name="variableList">List of variable declarations</param>
    Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        List<VariableClass> variableList);

    /// <summary>
    /// Set expression code and variable list directly
    /// </summary>
    /// <param name="code">Expression code with {varname} format</param>
    /// <param name="variableList">List of variable declarations</param>
    Task SetExpressionAsync(
        string code,
        List<VariableClass> variableList);
}

