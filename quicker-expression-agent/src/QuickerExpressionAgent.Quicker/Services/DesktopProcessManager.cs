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
    private readonly ILogger<DesktopProcessManager> _logger;
    private readonly DesktopAppConfig _config;

    public DesktopProcessManager(DesktopAppConfig config, ILogger<DesktopProcessManager> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Find process by process name (without .exe extension)
    /// Validates that the process is accessible and not exited
    /// </summary>
    /// <param name="processName">Process name (e.g., "QuickerExpressionAgent.Desktop")</param>
    /// <returns>First matching accessible process if found, null otherwise</returns>
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

            // Find first accessible process (not exited and can access MainModule)
            foreach (var process in processes)
            {
                try
                {
                    // Check if process has exited
                    if (process.HasExited)
                    {
                        process.Dispose();
                        continue;
                    }

                    // Try to access MainModule to verify process is accessible
                    // This will throw if process is from another user or inaccessible
                    var mainModule = process.MainModule;
                    if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
                    {
                        // Process is accessible, return it
                        return process;
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Process is not accessible (e.g., from another user), skip it
                    try { process.Dispose(); } catch { }
                    continue;
                }
                catch (InvalidOperationException)
                {
                    // Process has exited or is invalid, skip it
                    try { process.Dispose(); } catch { }
                    continue;
                }
            }

            // No accessible process found
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding process by name: {Name}", processName);
            return null;
        }
    }

    /// <summary>
    /// Find Desktop process by executable path or name
    /// Uses fast process name lookup first, then verifies path if needed
    /// </summary>
    /// <param name="desktopExePath">Optional executable path to verify against</param>
    /// <returns>Tuple containing the process and its executable path, or (null, null) if not found</returns>
    public (Process? process, string? exePath) FindDesktopProcess(string? desktopExePath = null)
    {
        // Fast path: find by process name first (much faster than iterating all processes)
        var process = FindProcessByName(_config.ProcessName);
        if (process == null || process.HasExited)
        {
            return (null, null);
        }

        string? exePath = null;
        try
        {
            exePath = process.MainModule?.FileName;
        }
        catch
        {
            // Ignore errors accessing process
            return (null, null);
        }

        // If a specific path was provided, verify it matches
        if (!string.IsNullOrEmpty(desktopExePath) && !string.IsNullOrEmpty(exePath))
        {
            var normalizedProvided = Path.GetFullPath(desktopExePath).ToLowerInvariant();
            var normalizedFound = Path.GetFullPath(exePath).ToLowerInvariant();

            if (normalizedProvided != normalizedFound)
            {
                // Path doesn't match, return null
                return (null, null);
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
            _logger.LogWarning(ex, "Error getting version from executable: {Path}", exePath);
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
            _logger.LogWarning(ex, "Error getting version from process: {ProcessId}", process.Id);
        }

        return null;
    }

    /// <summary>
    /// Check if process version matches required version
    /// </summary>
    /// <param name="process">Process to check</param>
    /// <param name="processExePath">Executable path of the process</param>
    /// <param name="requiredVersion">Required version as a string (e.g., "1.0.0")</param>
    /// <returns>True if version matches or no version check needed, false if version doesn't match</returns>
    public bool CheckVersionMatch(Process? process, string? processExePath, string? requiredVersion)
    {
        if (process == null || process.HasExited)
        {
            return false;
        }

        if (string.IsNullOrEmpty(requiredVersion))
        {
            // No version check needed
            return true;
        }

        if (string.IsNullOrEmpty(processExePath))
        {
            // Can't check version without exe path
            return false;
        }

        // Parse required version string
        if (!Version.TryParse(requiredVersion, out var requiredVersionObj))
        {
            // Invalid version string, assume mismatch
            return false;
        }

        var runningVersion = GetVersionFromExecutable(processExePath);
        if (runningVersion == null)
        {
            // Can't determine version, assume mismatch
            return false;
        }

        // Compare versions ignoring revision (4th component) if one is missing
        // e.g., "1.0.7.0" should match "1.0.7"
        return VersionsMatch(requiredVersionObj, runningVersion);
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
            _logger.LogInformation("Closing process: {ProcessId} ({Name})", process.Id, process.ProcessName);
            process.Kill();
            process.WaitForExit(timeout);
            return process.HasExited;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing process: {ProcessId}", process.Id);
            return false;
        }
    }

    /// <summary>
    /// Start Desktop application
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop executable</param>
    /// <param name="requiredVersion">Required version as a string (e.g., "1.0.0") - deprecated, version is now extracted from exe path</param>
    /// <returns>True if started successfully</returns>
    /// <exception cref="ArgumentException">Thrown if exe path is invalid</exception>
    /// <exception cref="FileNotFoundException">Thrown if exe file doesn't exist</exception>
    public bool StartDesktop(string desktopExePath, string? requiredVersion = null)
    {
        if (string.IsNullOrEmpty(desktopExePath))
        {
            throw new ArgumentException("Desktop executable path is required", nameof(desktopExePath));
        }

        if (!File.Exists(desktopExePath))
        {
            throw new FileNotFoundException($"Desktop executable not found: {desktopExePath}", desktopExePath);
        }

        // Version is now extracted from exe path in StartDesktopAsync, so we don't verify it here

        try
        {
            _logger.LogInformation("Starting Desktop application: {Path} (silent mode)", desktopExePath);

            // Normalize path to handle any path issues
            var normalizedPath = Path.GetFullPath(desktopExePath);
            var workingDir = Path.GetDirectoryName(normalizedPath);

            if (string.IsNullOrEmpty(workingDir))
            {
                throw new InvalidOperationException($"Invalid working directory for executable: {desktopExePath}");
            }

            // Try UseShellExecute = true first (recommended for .NET Framework)
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = normalizedPath,
                    Arguments = _config.SilentStartArgument,
                    UseShellExecute = true,
                    WorkingDirectory = workingDir,
                    Verb = "open" // Explicitly set verb to "open"
                };

                var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    _logger.LogInformation("Desktop application started successfully (PID: {ProcessId})", process.Id);
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Failed to start with UseShellExecute=true, trying alternative method");

                // Fallback: Try UseShellExecute = false (direct process creation)
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = normalizedPath,
                        Arguments = _config.SilentStartArgument,
                        UseShellExecute = false,
                        WorkingDirectory = workingDir,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    };

                    var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        _logger.LogInformation("Desktop application started successfully with alternative method (PID: {ProcessId})", process.Id);
                        return true;
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback method also failed to start Desktop application");
                    throw new InvalidOperationException($"Failed to start Desktop application: {ex.Message}. Fallback also failed: {fallbackEx.Message}", ex);
                }
            }

            throw new InvalidOperationException("Failed to start Desktop application: Process.Start returned null");
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw InvalidOperationException as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Desktop application: {Path}", desktopExePath);
            throw new InvalidOperationException($"Failed to start Desktop application: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Find the latest Desktop executable in Quicker packages directory
    /// Searches in MyDocuments/Quicker/_packages/{PackageId}/{version}/QuickerExpressionAgent.Desktop.exe
    /// </summary>
    /// <returns>Tuple containing (exePath, version) or (null, null) if not found</returns>
    public (string? exePath, string? version) FindLatestDesktopExe()
    {
        try
        {
            // Get packages directory from config
            var packagesDir = _config.GetPackagesDirectory();

            if (!Directory.Exists(packagesDir))
            {
                _logger.LogWarning("Quicker packages directory not found: {Path}", packagesDir);
                return (null, null);
            }

            // Get all subdirectories (version directories)
            var versionDirs = Directory.GetDirectories(packagesDir);
            if (versionDirs.Length == 0)
            {
                _logger.LogWarning("No version directories found in Quicker packages: {Path}", packagesDir);
                return (null, null);
            }

            // Find all directories that contain the Desktop executable
            var candidates = versionDirs
                .Select(dir =>
                {
                    var exePath = _config.GetExecutablePath(dir);
                    if (File.Exists(exePath))
                    {
                        // Extract version from directory name (last part of path)
                        var versionDirName = Path.GetFileName(dir);
                        return new
                        {
                            ExePath = exePath,
                            Version = versionDirName,
                            VersionObj = TryParseVersion(versionDirName)
                        };
                    }
                    return null;
                })
                .Where(x => x != null && x.VersionObj != null)
                .OrderByDescending(x => x!.VersionObj)
                .ToList();

            if (candidates.Count == 0)
            {
                return (null, null);
            }

            // Return the latest version
            var latest = candidates[0];
            return (latest.ExePath, latest.Version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error finding latest Desktop executable in packages directory");
            return (null, null);
        }
    }

    /// <summary>
    /// Compare two versions, ignoring revision (4th component) if one is missing
    /// e.g., "1.0.7.0" should match "1.0.7"
    /// </summary>
    /// <param name="version1">First version to compare</param>
    /// <param name="version2">Second version to compare</param>
    /// <returns>True if versions match (ignoring revision differences), false otherwise</returns>
    public bool VersionsMatch(Version version1, Version version2)
    {
        // Compare Major, Minor, and Build components
        // Ignore Revision (4th component) differences
        return version1.Major == version2.Major &&
               version1.Minor == version2.Minor &&
               version1.Build == version2.Build;
    }

    /// <summary>
    /// Try to parse version string to Version object for comparison
    /// </summary>
    /// <param name="versionString">Version string to parse</param>
    /// <returns>Version object if parsed successfully, null otherwise</returns>
    public Version? TryParseVersion(string versionString)
    {
        if (string.IsNullOrEmpty(versionString))
        {
            return null;
        }

        // Try direct parse first
        if (Version.TryParse(versionString, out var version))
        {
            return version;
        }

        // Try removing common prefixes/suffixes
        var cleaned = versionString.Trim();

        // Remove "v" prefix if present
        if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(1);
        }

        // Try parse again
        if (Version.TryParse(cleaned, out version))
        {
            return version;
        }

        return null;
    }
}

