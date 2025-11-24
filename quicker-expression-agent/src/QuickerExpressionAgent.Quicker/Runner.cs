using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using QuickerExpressionAgent.Quicker.Services;
using Quicker.Public;
using Quicker.Utilities;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Runner class for managing Quicker integration lifecycle
/// </summary>
public class Runner
{
    private readonly DesktopServiceClientConnector _desktopServiceClientConnector;
    private readonly Services.DesktopStartupService _desktopStartupService;
    private readonly string? _desktopExePath;

    /// <summary>
    /// Initialize Runner and start the launcher
    /// </summary>
    /// <param name="desktopExePath">Optional path to the Desktop application executable. If provided, will be used for validation in StartDesktopAsync.</param>
    /// <exception cref="InvalidOperationException">Thrown if launcher is already stopped or start fails</exception>
    public Runner(string? desktopExePath = null)
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
            _desktopStartupService = Launcher.GetService<Services.DesktopStartupService>();
            
            // Store desktop exe path for validation
            _desktopExePath = desktopExePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Runner: Launcher start failed or launcher is already stopped", ex);
        }
    }

    /// <summary>
    /// Ensure Desktop application is started and connected
    /// This method will check connection status and start Desktop if needed
    /// If desktopExePath was provided in constructor, will also verify version match even if already connected
    /// </summary>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if Desktop is connected, false otherwise</returns>
    private async Task<bool> EnsureDesktopConnectedAsync(TimeSpan? connectionTimeout = null)
    {
        return await _desktopStartupService.EnsureDesktopConnectedAsync(_desktopExePath, connectionTimeout);
    }

    /// <summary>
    /// Open chat window and optionally attach it to the specified window handle
    /// This method will ensure Desktop is started and connected before opening the chat window
    /// </summary>
    /// <param name="windowHandle">Window handle to attach chat window to. If null or invalid, opens chat window without attachment</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection if Desktop needs to be started (default: 30 seconds)</param>
    /// <returns>True if operation was successful</returns>
    /// <exception cref="InvalidOperationException">Thrown if Desktop service is not connected after attempting to start</exception>
    public async Task<bool> OpenChatWindowAsync(long? windowHandle = null, TimeSpan? connectionTimeout = null)
    {
        try
        {
            // Ensure Desktop is connected (will start if needed and verify version if exePath was provided)
            if (!await EnsureDesktopConnectedAsync(connectionTimeout))
            {
                throw new InvalidOperationException("Failed to start or connect to Desktop service. Please ensure .NET 8.0+ is installed and Desktop application can be started.");
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
    /// Start Desktop application and ensure connection
    /// </summary>
    /// <param name="desktopExePath">Path to the Desktop application executable. Must be QuickerExpressionAgent.Desktop.exe. If null, uses the path from constructor.</param>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if connection is established, false otherwise</returns>
    public async Task<bool> StartDesktopAsync(string? desktopExePath = null, TimeSpan? connectionTimeout = null)
    {
        // Use provided parameter, or fall back to stored exe path from constructor
        var exePathToUse = desktopExePath ?? _desktopExePath;
        if (string.IsNullOrEmpty(exePathToUse))
        {
            throw new ArgumentException("Desktop executable path is required. Provide it either in constructor or as parameter.", nameof(desktopExePath));
        }

        return await _desktopStartupService.StartDesktopAsync(exePathToUse, connectionTimeout);
    }

    /// <summary>
    /// Automatically find and start Desktop application from Quicker packages directory
    /// Searches for the latest version in MyDocuments/Quicker/_packages/{pkg_version}/QuickerExpressionAgent.Desktop.exe
    /// </summary>
    /// <param name="connectionTimeout">Maximum time to wait for connection (default: 30 seconds)</param>
    /// <returns>True if connection is established, false otherwise</returns>
    public async Task<bool> StartDesktopAutoAsync(TimeSpan? connectionTimeout = null)
    {
        return await _desktopStartupService.StartDesktopAutoAsync(connectionTimeout);
    }
}

