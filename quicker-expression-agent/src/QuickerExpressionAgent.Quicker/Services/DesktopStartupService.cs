using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Quicker;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Service for managing Desktop application startup, version checking, and connection
/// </summary>
public class DesktopStartupService
{
    private readonly DesktopServiceClientConnector _desktopServiceClientConnector;
    private readonly DesktopProcessManager _processManager;
    private readonly DotNetVersionChecker _dotNetVersionChecker;
    private readonly DesktopAppConfig _appConfig;
    private readonly ILogger<DesktopStartupService>? _logger;

    // Static dictionary to store version check dialog results (keyed by version combination)
    // Key format: "runningVersion->targetVersion"
    private static readonly Dictionary<string, VersionMismatchDialog.VersionChoice> _versionCheckDialogResults = new();

    public DesktopStartupService(
        DesktopServiceClientConnector desktopServiceClientConnector,
        DesktopProcessManager processManager,
        DotNetVersionChecker dotNetVersionChecker,
        DesktopAppConfig appConfig,
        ILogger<DesktopStartupService>? logger = null)
    {
        _desktopServiceClientConnector = desktopServiceClientConnector ?? throw new ArgumentNullException(nameof(desktopServiceClientConnector));
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _dotNetVersionChecker = dotNetVersionChecker ?? throw new ArgumentNullException(nameof(dotNetVersionChecker));
        _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        _logger = logger;
    }

    /// <summary>
    /// Ensure Desktop application is started and connected
    /// If desktopExePath is provided, will also verify version match even if already connected
    /// </summary>
    /// <param name="desktopExePath">Optional path to the Desktop application executable. If provided, will be used for version validation.</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if Desktop is connected, false otherwise</returns>
    public async Task<bool> EnsureDesktopConnectedAsync(string? desktopExePath = null, TimeSpan? connectionTimeout = null)
    {
        // Check if already connected
        if (_desktopServiceClientConnector.IsConnected)
        {
            // If desktopExePath was provided, verify version match even if already connected
            if (!string.IsNullOrEmpty(desktopExePath))
            {
                // Perform version check only
                return await CheckVersionAndRestartIfNeededAsync(desktopExePath, connectionTimeout);
            }
            
            // No exe path specified, already connected, return true
            return true;
        }

        // Not connected
        if (!string.IsNullOrEmpty(desktopExePath))
        {
            // Use StartDesktopAsync to start and ensure version match
            return await StartDesktopAsync(desktopExePath, connectionTimeout);
        }

        // No exe path specified, try to start Desktop automatically (no version check)
        return await StartDesktopAutoAsync(connectionTimeout);
    }

    /// <summary>
    /// Start Desktop application and ensure connection
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop application executable. Must be QuickerExpressionAgent.Desktop.exe</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if connection is established, false otherwise</returns>
    public async Task<bool> StartDesktopAsync(string desktopExePath, TimeSpan? connectionTimeout = null)
    {
        // Step 0: Check if .NET 8.0+ is installed
        if (!_dotNetVersionChecker.IsDotNet80Installed())
        {
            // Show install window on UI thread as dialog
            bool? dialogResult = false;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var installWindow = new DotNetInstallWindow(_dotNetVersionChecker.GetDownloadUrl());
                dialogResult = installWindow.ShowDialog();
            });
            
            // If user clicked "我已安装", check again
            if (dialogResult == true)
            {
                // Re-check after user confirmed installation
                if (!_dotNetVersionChecker.IsDotNet80Installed())
                {
                    return false;
                }
                // .NET is now installed, continue
            }
            else
            {
                // User cancelled or closed window
                return false;
            }
        }

        // Step 1: Validate desktopExePath and get version
        if (string.IsNullOrEmpty(desktopExePath))
        {
            throw new ArgumentException("Desktop executable path is required", nameof(desktopExePath));
        }

        // Check if file exists
        if (!System.IO.File.Exists(desktopExePath))
        {
            throw new System.IO.FileNotFoundException($"Desktop executable not found: {desktopExePath}", desktopExePath);
        }

        // Check if file name matches the expected Desktop executable name
        var fileName = System.IO.Path.GetFileName(desktopExePath);
        if (!string.Equals(fileName, _appConfig.ExecutableFileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Desktop executable must be {_appConfig.ExecutableFileName}, but got: {fileName}", nameof(desktopExePath));
        }

        // Get version from executable
        var targetVersion = _processManager.GetVersionFromExecutable(desktopExePath);
        if (targetVersion == null)
        {
            throw new InvalidOperationException($"Cannot get version from executable: {desktopExePath}");
        }

        var timeout = connectionTimeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        // Step 2: Check if already connected
        if (_desktopServiceClientConnector.IsConnected)
        {
            // Get running process and its version
            var (runningProcess, runningProcessPath) = _processManager.FindDesktopProcess(null);
            if (runningProcess != null && !runningProcess.HasExited && !string.IsNullOrEmpty(runningProcessPath))
            {
                var runningVersion = _processManager.GetVersionFromExecutable(runningProcessPath);
                if (runningVersion != null)
                {
                    // Check if versions match
                    if (!_processManager.VersionsMatch(targetVersion, runningVersion))
                    {
                        // Versions don't match, check if user wants to restart
                        var versionChoice = await CheckAndHandleVersionMismatchAsync(runningVersion, targetVersion);
                        if (versionChoice == null)
                        {
                            // Dialog was cancelled (should not happen with new dialog)
                            return false;
                        }

                        if (versionChoice == VersionMismatchDialog.VersionChoice.ContinueOldVersion)
                        {
                            // User chose to continue using old version, already connected
                            return true;
                        }

                        // User chose to start new version
                        // Shutdown old version gracefully
                        await ShutdownOldVersionAsync(runningProcess);
                        
                        // Reset start time for new process startup
                        startTime = DateTime.UtcNow;
                        
                        // Continue to start new version (fall through to Step 4)
                    }
                    else
                    {
                        // Versions match, already connected
                        return true;
                    }
                }
            }
        }

        // Step 3: Check if Desktop process is running (but not connected)
        var (desktopProcess, processPath) = _processManager.FindDesktopProcess(desktopExePath);
        if (desktopProcess != null && !desktopProcess.HasExited)
        {
            // Process is running, wait for connection using WaitConnectAsync
            var remainingTimeout = timeout - (DateTime.UtcNow - startTime);
            if (remainingTimeout > TimeSpan.Zero)
            {
                return await _desktopServiceClientConnector.WaitConnectAsync(remainingTimeout);
            }
            return false;
        }

        // Step 4: Process is not running, start it
        _processManager.StartDesktop(desktopExePath, null);

        // Wait a bit for the process to start
        await Task.Delay(1000);

        // Wait for connection using WaitConnectAsync
        var finalTimeout = timeout - (DateTime.UtcNow - startTime);
        if (finalTimeout > TimeSpan.Zero)
        {
            return await _desktopServiceClientConnector.WaitConnectAsync(finalTimeout);
        }

        return false;
    }

    /// <summary>
    /// Automatically find and start Desktop application from Quicker packages directory
    /// </summary>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if connection is established, false otherwise</returns>
    public async Task<bool> StartDesktopAutoAsync(TimeSpan? connectionTimeout = null)
    {
        var (exePath, version) = _processManager.FindLatestDesktopExe();
        if (exePath == null)
        {
            return false;
        }

        return await StartDesktopAsync(exePath, connectionTimeout);
    }

    /// <summary>
    /// Check version match and restart if needed (only when already connected)
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop application executable</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if version matches or restart successful, false otherwise</returns>
    private async Task<bool> CheckVersionAndRestartIfNeededAsync(string desktopExePath, TimeSpan? connectionTimeout = null)
    {
        // Validate and get version from executable
        if (!System.IO.File.Exists(desktopExePath))
        {
            return false;
        }

        var fileName = System.IO.Path.GetFileName(desktopExePath);
        if (!string.Equals(fileName, _appConfig.ExecutableFileName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var targetVersion = _processManager.GetVersionFromExecutable(desktopExePath);
        if (targetVersion == null)
        {
            return false;
        }

        // Get running process and its version
        var (runningProcess, runningProcessPath) = _processManager.FindDesktopProcess(null);
        if (runningProcess != null && !runningProcess.HasExited && !string.IsNullOrEmpty(runningProcessPath))
        {
            var runningVersion = _processManager.GetVersionFromExecutable(runningProcessPath);
            if (runningVersion != null)
            {
                // Check if versions match
                if (!_processManager.VersionsMatch(targetVersion, runningVersion))
                {
                    // Versions don't match, check if user wants to restart
                    var versionChoice = await CheckAndHandleVersionMismatchAsync(runningVersion, targetVersion);
                    if (versionChoice == null)
                    {
                        // Dialog was cancelled (should not happen with new dialog)
                        return false;
                    }

                    if (versionChoice == VersionMismatchDialog.VersionChoice.ContinueOldVersion)
                    {
                        // User chose to continue using old version, already connected
                        return true;
                    }

                    // User chose to start new version
                    // Shutdown old version gracefully
                    await ShutdownOldVersionAsync(runningProcess);
                    
                    // Start new version
                    _processManager.StartDesktop(desktopExePath, null);
                    await Task.Delay(1000);

                    // Wait for connection
                    var timeout = connectionTimeout ?? TimeSpan.FromSeconds(30);
                    return await _desktopServiceClientConnector.WaitConnectAsync(timeout);
                }
                else
                {
                    // Versions match, already connected
                    return true;
                }
            }
        }

        // Could not determine running version, assume OK
        return true;
    }

    /// <summary>
    /// Shutdown old version gracefully via service call
    /// If API call fails (e.g., old version doesn't implement ShutdownAsync), force kill the process
    /// </summary>
    private async Task ShutdownOldVersionAsync(System.Diagnostics.Process runningProcess)
    {
        bool gracefulShutdownAttempted = false;
        
        // Try graceful shutdown via API if connected
        if (_desktopServiceClientConnector.IsConnected)
        {
            try
            {
                // Try to call ShutdownAsync (may not exist in old versions)
                var serviceClient = _desktopServiceClientConnector.ServiceClient;
                if (serviceClient != null)
                {
                    gracefulShutdownAttempted = true;
                    
                    // Call ShutdownAsync with timeout to avoid waiting indefinitely
                    var shutdownTask = serviceClient.ShutdownAsync();
                    var shutdownCallTimeout = TimeSpan.FromSeconds(2);
                    var shutdownCallCompleted = await Task.WhenAny(shutdownTask, Task.Delay(shutdownCallTimeout)) == shutdownTask;
                    
                    _logger?.LogDebug("ShutdownAsync call completed: {Completed}", shutdownCallCompleted);
                }
            }
            catch (InvalidOperationException)
            {
                // Service client not connected, will force kill
                _logger?.LogDebug("Service client not connected, will force kill process");
            }
            catch (Exception ex)
            {
                // API call failed (e.g., method doesn't exist in old version, RPC error, etc.)
                _logger?.LogWarning(ex, "Failed to call ShutdownAsync (old version may not implement it), will force kill process");
            }
        }
        
        // Wait for process to exit (whether graceful or not)
        var shutdownTimeout = TimeSpan.FromSeconds(5);
        var shutdownStart = DateTime.UtcNow;
        while (!runningProcess.HasExited && DateTime.UtcNow - shutdownStart < shutdownTimeout)
        {
            await Task.Delay(100);
        }

        // If still running, force kill (always ensure process is terminated)
        if (!runningProcess.HasExited)
        {
            _logger?.LogInformation("Process did not exit gracefully, forcing kill. Graceful shutdown attempted: {Attempted}", gracefulShutdownAttempted);
            _processManager.CloseProcess(runningProcess);
            
            // Wait a bit more to ensure process is fully closed
            await Task.Delay(500);
            
            // Double check - if still running, try again
            if (!runningProcess.HasExited)
            {
                _logger?.LogWarning("Process still running after force kill, trying again");
                try
                {
                    runningProcess.Kill();
                    runningProcess.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error killing process");
                }
            }
        }
        else
        {
            _logger?.LogInformation("Process exited successfully. Graceful shutdown attempted: {Attempted}", gracefulShutdownAttempted);
            // Wait a bit to ensure process is fully closed
            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Check and handle version mismatch between running and target versions
    /// Shows confirmation dialog with options to start new version or continue using old version
    /// Result is cached per version combination
    /// </summary>
    /// <param name="runningVersion">Currently running version</param>
    /// <param name="targetVersion">Target version to start</param>
    /// <returns>User's choice: StartNewVersion, ContinueOldVersion, or null if cancelled</returns>
    private async Task<VersionMismatchDialog.VersionChoice?> CheckAndHandleVersionMismatchAsync(System.Version runningVersion, System.Version targetVersion)
    {
        // Create a key for this version combination
        var versionKey = $"{runningVersion}->{targetVersion}";
        
        // If user previously made a choice for this version combination, use cached result
        lock (_versionCheckDialogResults)
        {
            if (_versionCheckDialogResults.TryGetValue(versionKey, out var cachedResult))
            {
                return cachedResult;
            }
        }

        // Show confirmation dialog on UI thread
        VersionMismatchDialog.VersionChoice? dialogResult = null;
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var message = $"已经启动版本 {runningVersion}，是否退出并启动新版本 {targetVersion}？";
            var dialog = new VersionMismatchDialog(message);
            if (dialog.ShowDialog() == true && dialog.Result.HasValue)
            {
                dialogResult = dialog.Result.Value;
            }
        });

        // Cache the result if user made a choice
        if (dialogResult.HasValue)
        {
            lock (_versionCheckDialogResults)
            {
                _versionCheckDialogResults[versionKey] = dialogResult.Value;
            }
        }
        
        return dialogResult;
    }
}

