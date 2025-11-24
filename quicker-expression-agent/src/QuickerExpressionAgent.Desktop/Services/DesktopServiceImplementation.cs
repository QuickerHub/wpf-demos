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

    public DesktopServiceImplementation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Get the ChatWindow instance (create if not exists)
    /// </summary>
    private ChatWindow? GetChatWindow()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            // Try to find existing ChatWindow
            var existingWindow = Application.Current.Windows.OfType<ChatWindow>().FirstOrDefault();
            if (existingWindow != null)
            {
                return existingWindow;
            }

            // Create new ChatWindow if not exists
            try
            {
                var chatWindow = _serviceProvider.GetRequiredService<ChatWindow>();
                return chatWindow;
            }
            catch
            {
                return null;
            }
        });
    }

    public Task<bool> SendChatMessageAsync(string message)
    {
        return Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var chatWindow = GetChatWindow();
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    // Set the message and trigger generate command
                    chatWindow.ViewModel.ChatInputText = message;
                    chatWindow.ViewModel.GenerateCommand.Execute(null);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        });
    }

    public Task<bool> ShowChatWindowAsync(bool show)
    {
        return Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var chatWindow = GetChatWindow();
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    if (show)
                    {
                        chatWindow.ShowAndActivate();
                    }
                    else
                    {
                        chatWindow.Hide();
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        });
    }

    public Task<long> GetChatWindowHandleAsync()
    {
        return Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var chatWindow = GetChatWindow();
                    if (chatWindow == null)
                    {
                        return 0L;
                    }

                    var windowInteropHelper = new WindowInteropHelper(chatWindow);
                    return windowInteropHelper.Handle.ToInt64();
                }
                catch
                {
                    return 0L;
                }
            });
        });
    }

    public Task<bool> IsChatWindowConnectedAsync()
    {
        return Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var chatWindow = GetChatWindow();
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    return chatWindow.ViewModel.IsCodeEditorConnected;
                }
                catch
                {
                    return false;
                }
            });
        });
    }

    public async Task<bool> OpenChatWindowAsync(long? windowHandle = null)
    {
        return await Task.Run(async () =>
        {
            return await Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    var chatWindow = GetChatWindow();
                    if (chatWindow == null)
                    {
                        return false;
                    }

                    // Show and activate the chat window
                    chatWindow.ShowAndActivate();

                    // If windowHandle is provided and valid, attach to it
                    if (windowHandle.HasValue && windowHandle.Value != 0)
                    {
                        var targetWindowHandle = new IntPtr(windowHandle.Value);
                        
                        // Check if window handle is valid
                        if (WindowHelper.IsWindow(targetWindowHandle))
                        {
                            // Try to get handler ID from window handle to prevent auto-creation
                            // This will set CodeEditorHandlerId if the window is a CodeEditor
                            await chatWindow.ViewModel.SetCodeEditorHandlerIdFromWindowHandleAsync(windowHandle.Value);

                            // Use ChatWindow's built-in attachment method
                            chatWindow.AttachToWindow(targetWindowHandle, bringToForeground: true);
                        }
                        // If window handle is invalid, just show the window without attachment
                    }
                    // If windowHandle is null or 0, just show the window without attachment

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        });
    }

    public Task<bool> PingAsync()
    {
        return Task.FromResult(true);
    }
}

