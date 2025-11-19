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

    private static string GetPipeName(params string[] input)
    {
        return string.Join("_", [AppName, Configuration, .. input, VID]);
    }
}

