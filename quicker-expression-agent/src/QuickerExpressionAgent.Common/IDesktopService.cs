namespace QuickerExpressionAgent.Common;

/// <summary>
/// Service interface for Desktop application services
/// This interface is implemented by the Desktop project and called by the Quicker project
/// </summary>
public interface IDesktopService
{

    /// <summary>
    /// Open chat window and optionally attach it to the specified window handle
    /// </summary>
    /// <param name="windowHandle">Window handle to attach to (as long). If null or invalid, opens chat window without attachment</param>
    /// <returns>True if operation was successful</returns>
    Task<bool> OpenChatWindowAsync(long? windowHandle = null);

    /// <summary>
    /// Ping the desktop service to check if it's alive
    /// </summary>
    /// <returns>True if service is alive</returns>
    Task<bool> PingAsync();

    /// <summary>
    /// Shutdown the Desktop application gracefully
    /// </summary>
    /// <returns>True if shutdown was successful</returns>
    Task<bool> ShutdownAsync();

    /// <summary>
    /// Notify that a CodeEditorWindow was created/activated
    /// </summary>
    /// <param name="windowHandle">Window handle of the CodeEditorWindow (as long)</param>
    /// <returns>True if notification was processed successfully</returns>
    Task<bool> NotifyCodeEditorWindowCreatedAsync(long windowHandle);
}

