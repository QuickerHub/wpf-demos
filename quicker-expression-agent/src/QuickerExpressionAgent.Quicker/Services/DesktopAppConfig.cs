using System;
using System.IO;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Configuration for Desktop application paths and settings
/// </summary>
public class DesktopAppConfig
{
    /// <summary>
    /// Process name of the Desktop application (without .exe extension)
    /// </summary>
    public string ProcessName { get; set; } = "QuickerExpressionAgent.Desktop";

    /// <summary>
    /// Executable file name of the Desktop application
    /// </summary>
    public string ExecutableFileName { get; set; } = "QuickerExpressionAgent.Desktop.exe";

    /// <summary>
    /// Package ID for the Desktop application in Quicker packages directory
    /// </summary>
    public string PackageId { get; set; } = "cea.quicker-expression-agent.desktop";

    /// <summary>
    /// Silent start argument for the Desktop application
    /// </summary>
    public string SilentStartArgument { get; set; } = "--silent";

    /// <summary>
    /// Get the packages directory path where Desktop executables are stored
    /// Format: MyDocuments/Quicker/_packages/{PackageId}
    /// </summary>
    public string GetPackagesDirectory()
    {
        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(myDocuments, "Quicker", "_packages", PackageId);
    }

    /// <summary>
    /// Get the full path to the Desktop executable in a version directory
    /// </summary>
    /// <param name="versionDirectory">Version directory path</param>
    /// <returns>Full path to the executable</returns>
    public string GetExecutablePath(string versionDirectory)
    {
        return Path.Combine(versionDirectory, ExecutableFileName);
    }
}

