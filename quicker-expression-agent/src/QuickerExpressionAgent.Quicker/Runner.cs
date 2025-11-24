using System;
using System.Threading.Tasks;
using QuickerExpressionAgent.Quicker.Services;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Runner class for managing Quicker integration lifecycle
/// </summary>
public class Runner
{
    private readonly DesktopServiceClientConnector _desktopServiceClientConnector;
    private readonly DesktopProcessManager _processManager;

    /// <summary>
    /// Initialize Runner and start the launcher
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if launcher is already stopped or start fails</exception>
    public Runner()
    {
        try
        {
            // Start the launcher
            Launcher.Start();

            // Verify launcher is started
            if (Launcher.Status != LauncherStatus.Started)
            {
                throw new InvalidOperationException($"Launcher failed to start. Current status: {Launcher.Status}");
            }

            // Get services from DI
            _desktopServiceClientConnector = Launcher.GetService<DesktopServiceClientConnector>();
            _processManager = Launcher.GetService<DesktopProcessManager>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Runner: Launcher start failed or launcher is already stopped", ex);
        }
    }

    /// <summary>
    /// Open chat window and optionally attach it to the specified window handle
    /// </summary>
    /// <param name="windowHandle">Window handle to attach chat window to. If null or invalid, opens chat window without attachment</param>
    /// <returns>True if operation was successful</returns>
    /// <exception cref="InvalidOperationException">Thrown if Desktop service is not connected</exception>
    public async Task<bool> OpenChatWindowAsync(long? windowHandle = null)
    {
        try
        {
            // Wait for connection if not connected
            if (!_desktopServiceClientConnector.IsConnected)
            {
                // Wait up to 5 seconds for connection
                var timeout = TimeSpan.FromSeconds(5);
                var startTime = DateTime.UtcNow;
                while (!_desktopServiceClientConnector.IsConnected && DateTime.UtcNow - startTime < timeout)
                {
                    await Task.Delay(100);
                }

                if (!_desktopServiceClientConnector.IsConnected)
                {
                    throw new InvalidOperationException("Desktop service is not connected. Please ensure the Desktop application is running.");
                }
            }

            // Call Desktop service to open chat window
            return await _desktopServiceClientConnector.ServiceClient.OpenChatWindowAsync(windowHandle);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to open chat window", ex);
        }
    }

    /// <summary>
    /// Open chat window and optionally attach it to the specified window handle (synchronous wrapper)
    /// </summary>
    /// <param name="windowHandle">Window handle to attach chat window to. If null or invalid, opens chat window without attachment</param>
    /// <returns>True if operation was successful</returns>
    public bool OpenChatWindow(long? windowHandle = null)
    {
        return OpenChatWindowAsync(windowHandle).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Start Desktop application and ensure connection
    /// 1. Check if already connected to Desktop service
    /// 2. If not connected, check if Desktop process is running
    /// 3. If process is running, check version match (if requiredVersion is provided)
    /// 4. If version doesn't match, close old process and start new one
    /// 5. If process is not running, start it using the provided exe path
    /// 6. Wait for connection
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop application executable. Required if process is not running.</param>
    /// <param name="requiredVersion">Required version of the Desktop application. If provided, will check and restart if version doesn't match.</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if connection is established, false otherwise</returns>
    public async Task<bool> StartDesktopAsync(string? desktopExePath = null, Version? requiredVersion = null, TimeSpan? connectionTimeout = null)
    {
        var timeout = connectionTimeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        // Step 1: Check if already connected
        if (_desktopServiceClientConnector.IsConnected)
        {
            return true;
        }

        // Step 2: Check if Desktop process is running
        var (desktopProcess, runningProcessPath) = _processManager.FindDesktopProcess(desktopExePath);

        // Step 3: If process is running, check version match
        if (desktopProcess != null && !desktopProcess.HasExited)
        {
            // Check version match if required
            if (!_processManager.CheckVersionMatch(desktopProcess, runningProcessPath, requiredVersion))
            {
                // Version doesn't match, close old process
                _processManager.CloseProcess(desktopProcess);
                desktopProcess = null;
            }
            else
            {
                // Version matched or no version check, wait for connection
                while (!_desktopServiceClientConnector.IsConnected && DateTime.UtcNow - startTime < timeout)
                {
                    await Task.Delay(500);
                }

                return _desktopServiceClientConnector.IsConnected;
            }
        }

        // Step 4: Process is not running (or was closed due to version mismatch), start it
        if (string.IsNullOrEmpty(desktopExePath))
        {
            throw new ArgumentException("Desktop executable path is required when process is not running", nameof(desktopExePath));
        }

        // Start the Desktop application (version check is done inside StartDesktop)
        _processManager.StartDesktop(desktopExePath, requiredVersion);

        // Wait a bit for the process to start
        await Task.Delay(1000);

        // Wait for connection
        while (!_desktopServiceClientConnector.IsConnected && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(500);
        }

        return _desktopServiceClientConnector.IsConnected;
    }
}

