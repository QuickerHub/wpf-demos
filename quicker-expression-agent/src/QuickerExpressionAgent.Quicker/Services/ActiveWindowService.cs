using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quicker.Utilities;
using Quicker.View;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Service to monitor active window changes and notify Desktop when CodeEditorWindow is activated
/// </summary>
public class ActiveWindowService : IHostedService, IDisposable
{
    private readonly ILogger<ActiveWindowService> _logger;
    private readonly DesktopServiceClientConnector _desktopServiceConnector;
    private readonly ActiveWindowHook _activeWindowHook;

    public ActiveWindowService(
        ILogger<ActiveWindowService> logger,
        DesktopServiceClientConnector desktopServiceConnector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _desktopServiceConnector = desktopServiceConnector ?? throw new ArgumentNullException(nameof(desktopServiceConnector));
        _activeWindowHook = new ActiveWindowHook();
        
        // Subscribe to active window changes
        _activeWindowHook.ActiveWindowChanged += OnActiveWindowChanged;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // StartAsync is now guaranteed to run on UI thread via UiThreadHostedService wrapper
            _activeWindowHook.StartHook();
            _logger.LogInformation("ActiveWindowService started, monitoring foreground window changes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start ActiveWindowService");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void OnActiveWindowChanged(IntPtr windowHandle)
    {
        try
        {
            // Check if the foreground window is a CodeEditorWindow
            CheckAndNotifyCodeEditorWindow(windowHandle);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we're in an event handler
            _logger.LogError(ex, "Error in OnActiveWindowChanged for handle: {Handle}", windowHandle);
        }
    }

    private void CheckAndNotifyCodeEditorWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            // Check if this is a CodeEditorWindow by getting the WPF Window object
            var window = GetWindowFromHandle(windowHandle);
            
            if (window == null)
            {
                // Not a WPF window, silently skip
                return;
            }

            // Only process CodeEditorWindow, skip all other windows
            if (window is CodeEditorWindow)
            {
                _logger.LogInformation("CodeEditorWindow detected: {Handle}, notifying Desktop service", windowHandle);
                
                // Notify Desktop service asynchronously
                // DesktopServiceImplementation will check if ChatWindow already exists
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait for connection if not connected
                        if (!_desktopServiceConnector.IsConnected)
                        {
                            var connected = await _desktopServiceConnector.WaitConnectAsync(TimeSpan.FromSeconds(5));
                            if (!connected)
                            {
                                _logger.LogWarning("Desktop service not connected after waiting, cannot notify CodeEditorWindow creation");
                                return;
                            }
                        }

                        // Notify Desktop service
                        // DesktopServiceImplementation will check if this CodeEditorWindow already has a ChatWindow
                        var result = await _desktopServiceConnector.ServiceClient.NotifyCodeEditorWindowCreatedAsync(
                            windowHandle.ToInt64());

                        if (result)
                        {
                            _logger.LogInformation("Successfully notified Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to notify Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error notifying Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                    }
                });
            }
            // Silently skip non-CodeEditorWindow windows
        }
        catch (Exception ex)
        {
            // Only log errors, not warnings for non-CodeEditorWindow windows
            _logger.LogError(ex, "Error checking window handle {Handle}", windowHandle);
        }
    }

    /// <summary>
    /// Get WPF Window from window handle
    /// </summary>
    private Window? GetWindowFromHandle(IntPtr handle)
    {
        try
        {
            // First, try to get WPF Window from handle using HwndSource
            var source = HwndSource.FromHwnd(handle);
            if (source != null)
            {
                var window = source.RootVisual as Window;
                if (window != null)
                {
                    return window;
                }
            }

            // If not found via HwndSource, check Application.Current.Windows
            // This is a fallback for windows that might not have HwndSource
            return Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window w in Application.Current.Windows)
                {
                    try
                    {
                        var helper = new WindowInteropHelper(w);
                        if (helper.Handle == handle)
                        {
                            return w;
                        }
                    }
                    catch
                    {
                        // Silently ignore errors when checking window handles
                    }
                }
                
                return null;
            });
        }
        catch
        {
            // Silently return null if window cannot be retrieved
            return null;
        }
    }

    public void Dispose()
    {
        try
        {
            _activeWindowHook.ActiveWindowChanged -= OnActiveWindowChanged;
            _activeWindowHook.Dispose();
            _logger.LogInformation("ActiveWindowService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping ActiveWindowService");
        }
        GC.SuppressFinalize(this);
    }
}

