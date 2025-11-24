using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Service for managing Desktop application process lifecycle
/// Handles process finding, version checking, and starting
/// </summary>
public class DesktopProcessManager
{
    private readonly ILogger<DesktopProcessManager>? _logger;

    public DesktopProcessManager(ILogger<DesktopProcessManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Find process by executable path (case-insensitive, handles path normalization)
    /// </summary>
    /// <param name="exePath">Full path to the executable</param>
    /// <returns>Process if found, null otherwise</returns>
    public Process? FindProcessByExecutablePath(string exePath)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        try
        {
            var normalizedPath = Path.GetFullPath(exePath).ToLowerInvariant();
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                try
                {
                    // Try to get main module file name
                    var mainModule = process.MainModule;
                    if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
                    {
                        var processPath = Path.GetFullPath(mainModule.FileName).ToLowerInvariant();
                        if (processPath == normalizedPath)
                        {
                            return process;
                        }
                    }
                }
                catch
                {
                    // Ignore processes we can't access (e.g., system processes)
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error finding process by executable path: {Path}", exePath);
        }

        return null;
    }

    /// <summary>
    /// Find process by process name (without .exe extension)
    /// </summary>
    /// <param name="processName">Process name (e.g., "QuickerExpressionAgent.Desktop")</param>
    /// <returns>First matching process if found, null otherwise</returns>
    public Process? FindProcessByName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return null;
        }

        try
        {
            // Remove .exe extension if present
            var name = processName;
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }

            var processes = Process.GetProcessesByName(name);
            return processes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error finding process by name: {Name}", processName);
            return null;
        }
    }

    /// <summary>
    /// Find Desktop process by executable path or name
    /// </summary>
    /// <param name="desktopExePath">Optional executable path to search for</param>
    /// <returns>Tuple containing the process and its executable path, or (null, null) if not found</returns>
    public (Process? process, string? exePath) FindDesktopProcess(string? desktopExePath = null)
    {
        Process? process = null;
        string? exePath = null;

        // Try to find by executable path first (more precise)
        if (!string.IsNullOrEmpty(desktopExePath))
        {
            process = FindProcessByExecutablePath(desktopExePath);
            if (process != null)
            {
                exePath = desktopExePath;
                return (process, exePath);
            }
        }

        // Fallback: try to find by name
        process = FindProcessByName("QuickerExpressionAgent.Desktop");
        if (process != null && !process.HasExited)
        {
            try
            {
                exePath = process.MainModule?.FileName;
            }
            catch
            {
                // Ignore errors accessing process
            }
        }

        return (process, exePath);
    }

    /// <summary>
    /// Get version from executable file using FileVersionInfo
    /// This is more stable than calling the running process
    /// </summary>
    /// <param name="exePath">Path to the executable file</param>
    /// <returns>Version if found, null otherwise</returns>
    public Version? GetVersionFromExecutable(string exePath)
    {
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
            if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.FileVersion))
            {
                if (Version.TryParse(versionInfo.FileVersion, out var version))
                {
                    return version;
                }
            }

            // Fallback: try ProductVersion
            if (!string.IsNullOrEmpty(versionInfo?.ProductVersion))
            {
                if (Version.TryParse(versionInfo.ProductVersion, out var productVersion))
                {
                    return productVersion;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error getting version from executable: {Path}", exePath);
        }

        return null;
    }

    /// <summary>
    /// Get version from running process
    /// </summary>
    /// <param name="process">Process to get version from</param>
    /// <returns>Version if found, null otherwise</returns>
    public Version? GetVersionFromProcess(Process process)
    {
        if (process == null || process.HasExited)
        {
            return null;
        }

        try
        {
            var mainModule = process.MainModule;
            if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
            {
                return GetVersionFromExecutable(mainModule.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error getting version from process: {ProcessId}", process.Id);
        }

        return null;
    }

    /// <summary>
    /// Check if process version matches required version
    /// </summary>
    /// <param name="process">Process to check</param>
    /// <param name="processExePath">Executable path of the process</param>
    /// <param name="requiredVersion">Required version</param>
    /// <returns>True if version matches or no version check needed, false if version doesn't match</returns>
    public bool CheckVersionMatch(Process? process, string? processExePath, Version? requiredVersion)
    {
        if (process == null || process.HasExited)
        {
            return false;
        }

        if (requiredVersion == null)
        {
            // No version check needed
            return true;
        }

        if (string.IsNullOrEmpty(processExePath))
        {
            // Can't check version without exe path
            return false;
        }

        var runningVersion = GetVersionFromExecutable(processExePath);
        if (runningVersion == null)
        {
            // Can't determine version, assume mismatch
            return false;
        }

        return runningVersion == requiredVersion;
    }

    /// <summary>
    /// Close process gracefully
    /// </summary>
    /// <param name="process">Process to close</param>
    /// <param name="timeout">Timeout in milliseconds (default: 5000)</param>
    /// <returns>True if process was closed successfully</returns>
    public bool CloseProcess(Process? process, int timeout = 5000)
    {
        if (process == null || process.HasExited)
        {
            return true;
        }

        try
        {
            _logger?.LogInformation("Closing process: {ProcessId} ({Name})", process.Id, process.ProcessName);
            process.Kill();
            process.WaitForExit(timeout);
            return process.HasExited;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error closing process: {ProcessId}", process.Id);
            return false;
        }
    }

    /// <summary>
    /// Start Desktop application
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop executable</param>
    /// <param name="requiredVersion">Required version (will be verified before starting)</param>
    /// <returns>True if started successfully</returns>
    /// <exception cref="ArgumentException">Thrown if exe path is invalid</exception>
    /// <exception cref="FileNotFoundException">Thrown if exe file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">Thrown if version doesn't match</exception>
    public bool StartDesktop(string desktopExePath, Version? requiredVersion = null)
    {
        if (string.IsNullOrEmpty(desktopExePath))
        {
            throw new ArgumentException("Desktop executable path is required", nameof(desktopExePath));
        }

        if (!File.Exists(desktopExePath))
        {
            throw new FileNotFoundException($"Desktop executable not found: {desktopExePath}", desktopExePath);
        }

        // Verify version if required
        if (requiredVersion != null)
        {
            var exeVersion = GetVersionFromExecutable(desktopExePath);
            if (exeVersion == null)
            {
                throw new InvalidOperationException($"Failed to get version from executable: {desktopExePath}");
            }

            if (exeVersion != requiredVersion)
            {
                throw new InvalidOperationException(
                    $"Version mismatch: Required version {requiredVersion}, but executable version is {exeVersion}");
            }
        }

        try
        {
            _logger?.LogInformation("Starting Desktop application: {Path}", desktopExePath);
            var processStartInfo = new ProcessStartInfo
            {
                FileName = desktopExePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(desktopExePath)
            };

            Process.Start(processStartInfo);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start Desktop application: {Path}", desktopExePath);
            throw new InvalidOperationException($"Failed to start Desktop application: {ex.Message}", ex);
        }
    }
}

