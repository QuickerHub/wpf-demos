using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
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

    public DesktopServiceImplementation(
        IServiceProvider serviceProvider,
        ChatWindowService chatWindowService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _chatWindowService = chatWindowService ?? throw new ArgumentNullException(nameof(chatWindowService));
    }


    public async Task<bool> OpenChatWindowAsync(long? windowHandle = null)
    {
        return await Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                ChatWindow? chatWindow;

                if (windowHandle.HasValue && windowHandle.Value != 0)
                {
                    // Get or create ChatWindow for this specific CodeEditorWindow
                    chatWindow = _chatWindowService.GetOrCreateChatWindow(windowHandle.Value);
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    var targetWindowHandle = new IntPtr(windowHandle.Value);
                    
                    // Check if window handle is valid
                    if (WindowHelper.IsWindow(targetWindowHandle))
                    {
                        // Show at off-screen position to avoid flashing before attachment
                        chatWindow.ShowWithPosition(centerOnScreen: false);

                        // Try to get handler ID from window handle to prevent auto-creation
                        // This will set CodeEditorHandlerId if the window is a CodeEditor
                        await chatWindow.ViewModel.SetCodeEditorHandlerIdFromWindowHandleAsync(windowHandle.Value);

                        // Use ChatWindow's built-in attachment method
                        chatWindow.AttachToWindow(targetWindowHandle, bringToForeground: true);
                    }
                    else
                    {
                        // Invalid window handle, show as standalone (centered)
                        chatWindow.ShowWithPosition(centerOnScreen: true);
                    }
                }
                else
                {
                    // No specific window handle, get or create standalone ChatWindow
                    chatWindow = _chatWindowService.GetOrCreateStandaloneChatWindow();
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    // Show as standalone window (centered on screen)
                    chatWindow.ShowWithPosition(centerOnScreen: true);
                }

                return true;
            }
            catch
            {
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

