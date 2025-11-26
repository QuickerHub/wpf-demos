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
/// Service to monitor window creation events and notify Desktop when CodeEditorWindow is created
/// </summary>
public class WindowCreationService : IHostedService, IDisposable
{
    private readonly ILogger<WindowCreationService> _logger;
    private readonly DesktopServiceClientConnector _desktopServiceConnector;
    private readonly WindowCreationHook _windowCreationHook;
    private readonly HashSet<long> _notifiedWindows = new(); // Track windows that have been notified to avoid duplicates

    public WindowCreationService(
        ILogger<WindowCreationService> logger,
        DesktopServiceClientConnector desktopServiceConnector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _desktopServiceConnector = desktopServiceConnector ?? throw new ArgumentNullException(nameof(desktopServiceConnector));
        _windowCreationHook = new WindowCreationHook();
        
        // Subscribe to window creation events
        _windowCreationHook.WindowCreated += OnWindowCreated;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // StartAsync is now guaranteed to run on UI thread via UiThreadHostedService wrapper
            _windowCreationHook.StartHook();
            _logger.LogInformation("WindowCreationService started, monitoring window creation events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WindowCreationService");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void OnWindowCreated(IntPtr windowHandle)
    {
        // CRITICAL: Hook callbacks must return as quickly as possible
        // Do NOT block the hook callback thread - it will cause UI freezing
        // Queue the work to be processed asynchronously
        
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var windowHandleLong = windowHandle.ToInt64();

        // Quick check if already notified (fast path)
        lock (_notifiedWindows)
        {
            if (_notifiedWindows.Contains(windowHandleLong))
            {
                return; // Already notified, skip immediately
            }
        }

        // Process asynchronously without blocking the hook callback
        // Use Task.Run to avoid blocking the hook thread
        _ = Task.Run(async () =>
        {
            try
            {
                await CheckAndNotifyCodeEditorWindowAsync(windowHandle, windowHandleLong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnWindowCreated async handler for handle: {Handle}", windowHandle);
            }
        });
    }

    private async Task CheckAndNotifyCodeEditorWindowAsync(IntPtr windowHandle, long windowHandleLong)
    {
        // EVENT_OBJECT_CREATE is triggered when window is created, but window may not be fully shown yet
        // However, if window is already shown, we can get it immediately
        // Try to get window immediately first
        Window? window = null;
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                window = GetWindowFromHandle(windowHandle);
            }
            catch
            {
                // Silently ignore errors
            }
        }, System.Windows.Threading.DispatcherPriority.Normal);

        // If window not found, it might not be fully initialized yet
        // Wait a short time and retry once (most windows should be ready by then)
        if (window == null)
        {
            await Task.Delay(20); // Short delay for window initialization
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    window = GetWindowFromHandle(windowHandle);
                }
                catch
                {
                    // Silently ignore errors
                }
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        if (window == null)
        {
            // Not a WPF window, silently skip
            return;
        }

        // Only process CodeEditorWindow, skip all other windows
        if (window is CodeEditorWindow codeEditorWindow)
        {
            // Wait a short time for window content to be initialized
            await Task.Delay(50);
            
            // Check if content is empty or starts with "$="
            string content = string.Empty;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                content = codeEditorWindow.Text ?? string.Empty;
            }, System.Windows.Threading.DispatcherPriority.Normal);
            
            var trimmedContent = content.Trim();
            
            // If content is not empty and doesn't start with "$=", skip processing
            if (!string.IsNullOrEmpty(trimmedContent) && !trimmedContent.StartsWith("$="))
            {
                return;
            }
            
            // Mark as notified before sending notification to avoid race conditions
            lock (_notifiedWindows)
            {
                if (_notifiedWindows.Contains(windowHandleLong))
                {
                    return; // Another thread already notified
                }
                _notifiedWindows.Add(windowHandleLong);
            }

            _logger.LogInformation("CodeEditorWindow created: {Handle}, notifying Desktop service", windowHandle);
            
            try
            {
                // Wait for connection if not connected (with shorter timeout for faster response)
                if (!_desktopServiceConnector.IsConnected)
                {
                    var connected = await _desktopServiceConnector.WaitConnectAsync(TimeSpan.FromSeconds(2)); // Reduced from 5 to 2 seconds
                    if (!connected)
                    {
                        _logger.LogWarning("Desktop service not connected after waiting, cannot notify CodeEditorWindow creation");
                        // Remove from notified set so we can retry later
                        lock (_notifiedWindows)
                        {
                            _notifiedWindows.Remove(windowHandleLong);
                        }
                        return;
                    }
                }

                // Notify Desktop service
                // DesktopServiceImplementation will check if this CodeEditorWindow already has a ChatWindow
                var result = await _desktopServiceConnector.ServiceClient.NotifyCodeEditorWindowCreatedAsync(
                    windowHandleLong);

                if (result)
                {
                    _logger.LogInformation("Successfully notified Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                }
                else
                {
                    _logger.LogWarning("Failed to notify Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                    // Remove from notified set so we can retry later
                    lock (_notifiedWindows)
                    {
                        _notifiedWindows.Remove(windowHandleLong);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying Desktop service about CodeEditorWindow: {Handle}", windowHandle);
                // Remove from notified set so we can retry later
                lock (_notifiedWindows)
                {
                    _notifiedWindows.Remove(windowHandleLong);
                }
            }
        }
        // Silently skip non-CodeEditorWindow windows
    }

    /// <summary>
    /// Get WPF Window from window handle
    /// This method should be called from UI thread (via Dispatcher.InvokeAsync)
    /// </summary>
    private Window? GetWindowFromHandle(IntPtr handle)
    {
        try
        {
            // First, try to get WPF Window from handle using HwndSource
            // This is the fastest method and doesn't require UI thread
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
            // Note: This should only be called from UI thread
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
            _windowCreationHook.WindowCreated -= OnWindowCreated;
            _windowCreationHook.Dispose();
            lock (_notifiedWindows)
            {
                _notifiedWindows.Clear();
            }
            _logger.LogInformation("WindowCreationService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping WindowCreationService");
        }
        GC.SuppressFinalize(this);
    }
}

