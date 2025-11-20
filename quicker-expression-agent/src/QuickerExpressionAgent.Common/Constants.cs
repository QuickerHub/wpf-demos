namespace QuickerExpressionAgent.Common;

/// <summary>
/// Constants for IPC communication
/// </summary>
public static class Constants
{
    private const string Configuration =
#if DEBUG
        "Debug";
#elif RELEASE
        "Release";
#endif

    private const string VID = "QEA2026"; // Prevent pipe name conflicts
    private const string AppName = "QuickerExpressionAgent";

    public static readonly string ServerName = GetPipeName("Server");

    /// <summary>
    /// Mutex name for ensuring only one instance of the application is running
    /// </summary>
    public static readonly string AppMutexName = GetPipeName("AppMutex");

    private static string GetPipeName(params string[] input)
    {
        return string.Join("_", [AppName, .. input, VID]);
    }
}
