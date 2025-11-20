using System.Threading.Tasks;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Implementation of IQuickerService for the Quicker integration
/// </summary>
[H.IpcGenerators.IpcServer]
public partial class QuickerServiceImplementation : IQuickerService
{
    /// <summary>
    /// Execute a C# expression
    /// </summary>
    public Task<ExpressionResult> ExecuteExpressionAsync(ExpressionRequest request)
    {
        // TODO: Implement expression execution logic
        return Task.FromResult(new ExpressionResult
        {
            Success = false,
            Error = "Not implemented yet"
        });
    }

    /// <summary>
    /// Set expression code and variable list
    /// </summary>
    public Task SetExpressionAsync(ExpressionRequest request)
    {
        // TODO: Implement expression setting logic
        return Task.CompletedTask;
    }
}

