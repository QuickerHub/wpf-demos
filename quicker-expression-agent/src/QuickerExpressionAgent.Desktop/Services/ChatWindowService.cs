using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Extensions;
using QuickerExpressionAgent.Server.Services;
using WindowAttach.Utils;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service to manage ChatWindow instances, ensuring one ChatWindow per CodeEditorWindow
/// </summary>
public class ChatWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatWindowService> _logger;
    private readonly QuickerServerClientConnector _quickerConnector;
    private readonly Dictionary<long, ChatWindow> _chatWindowsByCodeEditorHandle = new();
    private readonly object _lockObject = new object();

    public ChatWindowService(
        IServiceProvider serviceProvider,
        QuickerServerClientConnector quickerConnector,
        ILogger<ChatWindowService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _quickerConnector = quickerConnector ?? throw new ArgumentNullException(nameof(quickerConnector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle CodeEditorWindow created - automatically open ChatWindow and attach to CodeEditorWindow
    /// Called by DesktopServiceImplementation when a CodeEditorWindow is created/activated
    /// </summary>
    /// <param name="windowHandle">Window handle of the CodeEditorWindow (as long)</param>
    public async Task HandleCodeEditorWindowCreatedAsync(long windowHandle)
    {
        try
        {
            _logger.LogDebug("CodeEditorWindow created: {Handle}, opening ChatWindow", windowHandle);

            // Check if ChatWindow already exists for this CodeEditorWindow
            if (HasChatWindow(windowHandle))
            {
                _logger.LogDebug("ChatWindow already exists for CodeEditorWindow: {Handle}, skipping", windowHandle);
                return;
            }

            // Get or create ChatWindow for this CodeEditorWindow
            var chatWindow = GetOrCreateChatWindow(windowHandle);
            if (chatWindow == null)
            {
                _logger.LogWarning("Failed to create ChatWindow for CodeEditorWindow: {Handle}", windowHandle);
                return;
            }

            // Verify window handle is valid
            var targetWindowHandle = new IntPtr(windowHandle);
            if (!WindowHelper.IsWindow(targetWindowHandle))
            {
                _logger.LogWarning("Invalid window handle for CodeEditorWindow: {Handle}", windowHandle);
                return;
            }

            // Show and attach ChatWindow to CodeEditorWindow
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                chatWindow.ShowAndActivate();
            });

            // Set CodeEditor handler ID from window handle
            await chatWindow.ViewModel.SetCodeEditorHandlerIdFromWindowHandleAsync(windowHandle);

            // Attach ChatWindow to CodeEditorWindow
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                chatWindow.AttachToWindow(targetWindowHandle, bringToForeground: true);
            });

            _logger.LogDebug("Successfully opened ChatWindow for CodeEditorWindow: {Handle}", windowHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening ChatWindow for CodeEditorWindow {Handle}", windowHandle);
        }
    }

    /// <summary>
    /// Get or create a ChatWindow for the specified CodeEditorWindow handle
    /// Ensures one ChatWindow per CodeEditorWindow
    /// </summary>
    /// <param name="codeEditorWindowHandle">Window handle of the CodeEditorWindow (as long)</param>
    /// <returns>ChatWindow instance, or null if failed</returns>
    public ChatWindow? GetOrCreateChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
        {
            _logger.LogWarning("Invalid CodeEditorWindow handle: 0");
            return null;
        }

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Check if ChatWindow already exists for this CodeEditorWindow
                if (_chatWindowsByCodeEditorHandle.TryGetValue(codeEditorWindowHandle, out var existingChatWindow))
                {
                    // Verify the window still exists
                    if (IsWindowValid(existingChatWindow))
                    {
                        _logger.LogDebug("Found existing ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                        return existingChatWindow;
                    }
                    else
                    {
                        // Window was closed, remove from dictionary
                        _logger.LogDebug("Existing ChatWindow was closed, removing from dictionary: {Handle}", codeEditorWindowHandle);
                        _chatWindowsByCodeEditorHandle.Remove(codeEditorWindowHandle);
                    }
                }

                // Create new ChatWindow
                try
                {
                    var chatWindow = _serviceProvider.GetRequiredService<ChatWindow>();
                    
                    // Subscribe to Closed event to remove from dictionary
                    chatWindow.Closed -= ChatWindow_Closed;
                    chatWindow.Closed += ChatWindow_Closed;

                    // Add to dictionary
                    _chatWindowsByCodeEditorHandle[codeEditorWindowHandle] = chatWindow;
                    _logger.LogDebug("Created new ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);

                    return chatWindow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                    return null;
                }
            }
        });
    }

    /// <summary>
    /// Get ChatWindow for the specified CodeEditorWindow handle (if exists)
    /// </summary>
    /// <param name="codeEditorWindowHandle">Window handle of the CodeEditorWindow (as long)</param>
    /// <returns>ChatWindow instance if exists, null otherwise</returns>
    public ChatWindow? GetChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
            return null;

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                if (_chatWindowsByCodeEditorHandle.TryGetValue(codeEditorWindowHandle, out var chatWindow))
                {
                    if (IsWindowValid(chatWindow))
                    {
                        return chatWindow;
                    }
                    else
                    {
                        // Window was closed, remove from dictionary
                        _chatWindowsByCodeEditorHandle.Remove(codeEditorWindowHandle);
                    }
                }
                return null;
            }
        });
    }

    /// <summary>
    /// Check if a ChatWindow exists for the specified CodeEditorWindow handle
    /// </summary>
    /// <param name="codeEditorWindowHandle">Window handle of the CodeEditorWindow (as long)</param>
    /// <returns>True if ChatWindow exists and is valid, false otherwise</returns>
    public bool HasChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
            return false;

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                if (_chatWindowsByCodeEditorHandle.TryGetValue(codeEditorWindowHandle, out var chatWindow))
                {
                    if (IsWindowValid(chatWindow))
                    {
                        return true;
                    }
                    else
                    {
                        // Window was closed, remove from dictionary
                        _chatWindowsByCodeEditorHandle.Remove(codeEditorWindowHandle);
                    }
                }
                return false;
            }
        });
    }

    /// <summary>
    /// Get all active ChatWindow instances
    /// </summary>
    public IReadOnlyList<ChatWindow> GetAllChatWindows()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Clean up invalid windows and return valid ones
                var validWindows = _chatWindowsByCodeEditorHandle
                    .Where(kvp => IsWindowValid(kvp.Value))
                    .Select(kvp => kvp.Value)
                    .ToList();

                // Remove invalid windows
                var invalidHandles = _chatWindowsByCodeEditorHandle
                    .Where(kvp => !IsWindowValid(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var handle in invalidHandles)
                {
                    _chatWindowsByCodeEditorHandle.Remove(handle);
                }

                return validWindows;
            }
        });
    }

    /// <summary>
    /// Get or create a standalone ChatWindow (not attached to any CodeEditorWindow)
    /// If a standalone ChatWindow already exists, returns it; otherwise creates a new one
    /// Note: When ChatWindow creates a CodeEditorWindow, it should call RegisterChatWindowForCodeEditor
    /// to register the association so that HasChatWindow can detect it
    /// </summary>
    /// <returns>ChatWindow instance, or null if failed</returns>
    public ChatWindow? GetOrCreateStandaloneChatWindow()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // First, try to find an existing standalone ChatWindow (not in dictionary)
                var allWindows = Application.Current.Windows.OfType<ChatWindow>().ToList();
                var standaloneWindow = allWindows.FirstOrDefault(w => 
                    !_chatWindowsByCodeEditorHandle.Values.Contains(w) && IsWindowValid(w));

                if (standaloneWindow != null)
                {
                    _logger.LogDebug("Found existing standalone ChatWindow");
                    return standaloneWindow;
                }

                // Create new standalone ChatWindow
                try
                {
                    var chatWindow = _serviceProvider.GetRequiredService<ChatWindow>();
                    _logger.LogDebug("Created new standalone ChatWindow");
                    return chatWindow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create standalone ChatWindow");
                    return null;
                }
            }
        });
    }

    /// <summary>
    /// Register a ChatWindow for a CodeEditorWindow handle
    /// This should be called when a ChatWindow creates a CodeEditorWindow and gets its handle
    /// </summary>
    /// <param name="chatWindow">ChatWindow instance</param>
    /// <param name="codeEditorWindowHandle">Window handle of the CodeEditorWindow (as long)</param>
    public void RegisterChatWindowForCodeEditor(ChatWindow chatWindow, long codeEditorWindowHandle)
    {
        if (chatWindow == null)
            return;

        if (codeEditorWindowHandle == 0)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Check if this CodeEditorWindow already has a ChatWindow
                if (_chatWindowsByCodeEditorHandle.TryGetValue(codeEditorWindowHandle, out var existingChatWindow))
                {
                    // If it's the same ChatWindow, no need to register again
                    if (existingChatWindow == chatWindow)
                    {
                        _logger.LogDebug("ChatWindow already registered for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                        return;
                    }

                    // If it's a different ChatWindow, log a warning but still register (replace)
                    _logger.LogWarning("CodeEditorWindow {Handle} already has a ChatWindow, replacing with new one", codeEditorWindowHandle);
                }

                // Register the association
                _chatWindowsByCodeEditorHandle[codeEditorWindowHandle] = chatWindow;
                _logger.LogDebug("Registered ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);

                // Ensure Closed event is subscribed to remove from dictionary
                chatWindow.Closed -= ChatWindow_Closed;
                chatWindow.Closed += ChatWindow_Closed;
            }
        });
    }

    /// <summary>
    /// Handler for ChatWindow Closed event to remove from dictionary
    /// </summary>
    private void ChatWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is ChatWindow chatWindow)
        {
            lock (_lockObject)
            {
                // Remove from dictionary when window is closed
                var handleToRemove = _chatWindowsByCodeEditorHandle
                    .FirstOrDefault(kvp => kvp.Value == chatWindow).Key;
                if (handleToRemove != 0)
                {
                    _chatWindowsByCodeEditorHandle.Remove(handleToRemove);
                    _logger.LogDebug("Removed ChatWindow from dictionary for CodeEditorWindow: {Handle}", handleToRemove);
                }
            }
        }
    }

    /// <summary>
    /// Get an available CodeEditorWindow that doesn't have a ChatWindow yet
    /// Tries to find an existing CodeEditorWindow without ChatWindow, or creates a new one
    /// </summary>
    /// <returns>Window handle of the available CodeEditorWindow, or 0 if failed or all CodeEditorWindows already have ChatWindows</returns>
    public async Task<long> GetAvailableCodeEditorWindowHandleAsync()
    {
        try
        {
            // Wait for connection if not connected
            if (!_quickerConnector.IsConnected)
            {
                var connected = await _quickerConnector.WaitConnectAsync(TimeSpan.FromSeconds(5));
                if (!connected)
                {
                    _logger.LogWarning("Quicker service not connected, cannot get CodeEditorWindow");
                    return 0;
                }
            }

            // Try to get or create a CodeEditorWindow
            // Note: GetOrCreateCodeEditorAsync may return an existing CodeEditorWindow
            var handlerId = await _quickerConnector.ServiceClient.GetOrCreateCodeEditorAsync();
            if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
            {
                _logger.LogWarning("Failed to get or create CodeEditorWindow");
                return 0;
            }

            // Get window handle for this handler
            var windowHandle = await _quickerConnector.ServiceClient.GetWindowHandleAsync(handlerId);
            if (windowHandle == 0)
            {
                _logger.LogWarning("Failed to get window handle for CodeEditorWindow handler: {HandlerId}", handlerId);
                return 0;
            }

            // Check if this CodeEditorWindow already has a ChatWindow
            if (HasChatWindow(windowHandle))
            {
                _logger.LogDebug("CodeEditorWindow {Handle} already has a ChatWindow, returning 0 to indicate no available window", windowHandle);
                // This CodeEditorWindow already has a ChatWindow
                // Return 0 to indicate no available CodeEditorWindow
                // The caller (ChatWindowViewModel) will handle creating a new CodeEditorWindow if needed
                return 0;
            }

            _logger.LogDebug("Found available CodeEditorWindow: {Handle}", windowHandle);
            return windowHandle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available CodeEditorWindow");
            return 0;
        }
    }

    /// <summary>
    /// Check if a window is still valid (exists and not closed)
    /// </summary>
    private bool IsWindowValid(Window window)
    {
        try
        {
            if (window == null)
                return false;

            // Check if window is still in Application.Windows collection
            if (!Application.Current.Windows.OfType<ChatWindow>().Contains(window))
                return false;

            // Check if window handle is valid
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                return false;

            return WindowHelper.IsWindow(helper.Handle);
        }
        catch
        {
            return false;
        }
    }
}

