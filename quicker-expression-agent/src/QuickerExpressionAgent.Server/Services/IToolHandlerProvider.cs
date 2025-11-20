using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Provides IExpressionAgentToolHandler that can be modified at runtime
/// </summary>
public interface IToolHandlerProvider
{
    /// <summary>
    /// Gets the current IExpressionAgentToolHandler instance
    /// </summary>
    IExpressionAgentToolHandler ToolHandler { get; }
}

