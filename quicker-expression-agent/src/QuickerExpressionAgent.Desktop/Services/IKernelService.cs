using Microsoft.SemanticKernel;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for managing Semantic Kernel instance
/// </summary>
public interface IKernelService
{
    /// <summary>
    /// Gets the configured Kernel instance
    /// </summary>
    Kernel Kernel { get; }
}

