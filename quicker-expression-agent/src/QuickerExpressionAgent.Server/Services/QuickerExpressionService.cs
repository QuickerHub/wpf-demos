using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Service implementation for executing expressions in Quicker via H.Ipc
/// </summary>
public class QuickerExpressionService : IQuickerExpressionService
{
    private readonly QuickerServerClientConnector _connector;

    public QuickerExpressionService(QuickerServerClientConnector connector)
    {
        _connector = connector;
    }

    /// <summary>
    /// Execute a C# expression in Quicker
    /// Expression uses {varname} format, which will be replaced with actual variable names during execution
    /// </summary>
    /// <param name="code">Expression code with {varname} format</param>
    /// <param name="variableList">List of variable declarations</param>
    public async Task<ExpressionResult> ExecuteExpressionAsync(
        string code,
        List<VariableClass> variableList)
    {
        var request = new ExpressionRequest
        {
            Code = code,
            VariableList = variableList
        };
        return await _connector.ServiceClient.ExecuteExpressionAsync(request);
    }

    /// <summary>
    /// Set expression code and variable list directly
    /// </summary>
    /// <param name="code">Expression code with {varname} format</param>
    /// <param name="variableList">List of variable declarations</param>
    public async Task SetExpressionAsync(
        string code,
        List<VariableClass> variableList)
    {
        var request = new ExpressionRequest
        {
            Code = code,
            VariableList = variableList
        };
        await _connector.ServiceClient.SetExpressionAsync(request);
    }
}

