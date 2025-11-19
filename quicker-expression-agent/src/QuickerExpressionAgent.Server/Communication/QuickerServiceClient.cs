using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Communication;

/// <summary>
/// Client for communicating with Quicker Expression Service via H.Ipc
/// </summary>
public class QuickerServiceClient : IDisposable
{
    private readonly ExpressionServiceClient _client;

    public QuickerServiceClient()
    {
        _client = new ExpressionServiceClient();
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
        return await _client.ExecuteExpressionAsync(request);
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
        await _client.SetExpressionAsync(request);
    }

    public void Dispose()
    {
        // H.Ipc client handles disposal automatically
    }
}

