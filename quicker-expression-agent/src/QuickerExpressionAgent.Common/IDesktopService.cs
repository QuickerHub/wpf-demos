namespace QuickerExpressionAgent.Common;

/// <summary>
/// Service interface for Desktop application services
/// This interface is implemented by the Desktop project and called by the Quicker project
/// </summary>
public interface IDesktopService
{
    /// <summary>
    /// Send a message to the chat window
    /// </summary>
    /// <param name="message">Message content to send</param>
    /// <returns>True if message was sent successfully</returns>
    Task<bool> SendChatMessageAsync(string message);

    /// <summary>
    /// Show or hide the chat window
    /// </summary>
    /// <param name="show">True to show, false to hide</param>
    /// <returns>True if operation was successful</returns>
    Task<bool> ShowChatWindowAsync(bool show);

    /// <summary>
    /// Get the chat window handle
    /// </summary>
    /// <returns>Window handle as long, or 0 if not available</returns>
    Task<long> GetChatWindowHandleAsync();

    /// <summary>
    /// Check if the chat window is connected to a code editor
    /// </summary>
    /// <returns>True if connected, false otherwise</returns>
    Task<bool> IsChatWindowConnectedAsync();

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
}

