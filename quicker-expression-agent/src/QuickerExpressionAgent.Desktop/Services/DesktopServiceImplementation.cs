using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Extensions;
using QuickerExpressionAgent.Desktop.ViewModels;
using WindowAttach.Utils;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Implementation of IDesktopService for the Desktop application
/// </summary>
public class DesktopServiceImplementation : IDesktopService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ChatWindowService _chatWindowService;
    private readonly QuickerServerClientConnector _quickerConnector;
    private readonly ILogger<DesktopServiceImplementation> _logger;

    public DesktopServiceImplementation(
        IServiceProvider serviceProvider,
        ChatWindowService chatWindowService,
        QuickerServerClientConnector quickerConnector,
        ILogger<DesktopServiceImplementation> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _chatWindowService = chatWindowService ?? throw new ArgumentNullException(nameof(chatWindowService));
        _quickerConnector = quickerConnector ?? throw new ArgumentNullException(nameof(quickerConnector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task<bool> OpenChatWindowAsync(long? windowHandle = null)
    {
        return await Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                // Directly open CodeEditorWindow instead of ChatWindow to avoid opening two windows
                if (windowHandle.HasValue && windowHandle.Value != 0)
                {
                    // Activate the specified CodeEditorWindow by window handle
                    var targetWindowHandle = new IntPtr(windowHandle.Value);
                    
                    // Check if window handle is valid
                    if (WindowHelper.IsWindow(targetWindowHandle))
                    {
                        // Bring the CodeEditorWindow to foreground
                        WindowHelper.BringWindowToForeground(targetWindowHandle);
                        _logger.LogDebug("Activated CodeEditorWindow with handle: {Handle}", windowHandle.Value);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid window handle: {Handle}", windowHandle.Value);
                        return false;
                    }
                }
                else
                {
                    // No specific window handle, get or create CodeEditorWindow
                    // Wait for connection if not connected
                    if (!_quickerConnector.IsConnected)
                    {
                        var connected = await _quickerConnector.WaitConnectAsync(TimeSpan.FromSeconds(5));
                        if (!connected)
                        {
                            _logger.LogWarning("Quicker service not connected, cannot open CodeEditorWindow");
                            return false;
                        }
                    }

                    // Get or create CodeEditorWindow
                    var handlerId = await _quickerConnector.ServiceClient.GetOrCreateCodeEditorAsync();
                    if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                    {
                        _logger.LogWarning("Failed to get or create CodeEditorWindow");
                        return false;
                    }

                    // Get window handle for this handler and bring it to foreground
                    var codeEditorWindowHandle = await _quickerConnector.ServiceClient.GetWindowHandleAsync(handlerId);
                    if (codeEditorWindowHandle != 0)
                    {
                        var targetWindowHandle = new IntPtr(codeEditorWindowHandle);
                        if (WindowHelper.IsWindow(targetWindowHandle))
                        {
                            WindowHelper.BringWindowToForeground(targetWindowHandle);
                            _logger.LogDebug("Opened and activated CodeEditorWindow with handle: {Handle}", codeEditorWindowHandle);
                            return true;
                        }
                    }

                    _logger.LogWarning("Failed to get window handle for CodeEditorWindow handler: {HandlerId}", handlerId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening CodeEditorWindow");
                return false;
            }
        });
    }

    public Task<bool> PingAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> ShutdownAsync()
    {
        // Use BeginInvoke to asynchronously trigger shutdown
        // This ensures the return value can be sent before the connection closes
        try
        {
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                try
                {
                    // Shutdown the application gracefully
                    Application.Current.Shutdown();
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
            
            // Return immediately, shutdown will happen asynchronously
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> NotifyCodeEditorWindowCreatedAsync(long windowHandle)
    {
        try
        {
            // Directly call ChatWindowService to handle CodeEditorWindow creation
            // This avoids circular dependency and event subscription complexity
            await _chatWindowService.HandleCodeEditorWindowCreatedAsync(windowHandle);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

