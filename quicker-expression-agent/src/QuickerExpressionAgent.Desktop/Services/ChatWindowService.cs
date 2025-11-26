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
/// Simplified registration mechanism to reduce complexity
/// </summary>
public class ChatWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatWindowService> _logger;
    private readonly QuickerServerClientConnector _quickerConnector;
    private readonly object _lockObject = new object();

    /// <summary>
    /// Main registration table: CodeEditorWindow handle -> ChatWindow
    /// </summary>
    private readonly Dictionary<long, ChatWindow> _registrations = new();

    /// <summary>
    /// ChatWindows waiting for CodeEditorWindow creation
    /// </summary>
    private readonly HashSet<ChatWindow> _waitingChatWindows = new();

    /// <summary>
    /// CodeEditorWindow handles currently being processed (to prevent duplicate creation)
    /// </summary>
    private readonly HashSet<long> _processingHandles = new();

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
    /// Unified method to register or get ChatWindow for a CodeEditorWindow
    /// This is the single entry point for all registration operations
    /// </summary>
    /// <param name="codeEditorWindowHandle">Window handle of the CodeEditorWindow (0 if not yet created)</param>
    /// <param name="chatWindow">ChatWindow instance (null if needs to be created)</param>
    /// <param name="isCreatingCodeEditor">True if ChatWindow is creating CodeEditorWindow</param>
    /// <returns>ChatWindow instance, or null if failed or needs to be created</returns>
    private ChatWindow? RegisterOrGetChatWindow(long codeEditorWindowHandle, ChatWindow? chatWindow, bool isCreatingCodeEditor)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Case 1: CodeEditorWindow already has a registered ChatWindow
                if (codeEditorWindowHandle != 0 && _registrations.TryGetValue(codeEditorWindowHandle, out var existing))
                {
                    if (IsWindowValid(existing))
                    {
                        _logger.LogDebug("CodeEditorWindow {Handle} already has ChatWindow", codeEditorWindowHandle);
                        return existing;
                    }
                    else
                    {
                        // Window was closed, remove invalid registration
                        _registrations.Remove(codeEditorWindowHandle);
                    }
                }

                // Case 2: ChatWindow is creating CodeEditorWindow (mark as waiting)
                if (isCreatingCodeEditor && chatWindow != null)
                {
                    if (_waitingChatWindows.Contains(chatWindow))
                    {
                        _logger.LogDebug("ChatWindow already marked as waiting");
                        return chatWindow;
                    }

                    _waitingChatWindows.Add(chatWindow);
                    chatWindow.Closed -= ChatWindow_Closed;
                    chatWindow.Closed += ChatWindow_Closed;
                    
                    _logger.LogDebug("Marked ChatWindow as waiting for CodeEditorWindow creation");
                    return chatWindow;
                }

                // Case 3: CodeEditorWindow created, check if there's a waiting ChatWindow
                if (codeEditorWindowHandle != 0)
                {
                    // Find a waiting ChatWindow (FIFO: first waiting gets it)
                    var waitingChatWindow = _waitingChatWindows.FirstOrDefault(w => IsWindowValid(w));
                    if (waitingChatWindow != null)
                    {
                        _waitingChatWindows.Remove(waitingChatWindow);
                        _registrations[codeEditorWindowHandle] = waitingChatWindow;
                        _logger.LogDebug("Assigned waiting ChatWindow to CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                        return waitingChatWindow;
                    }

                    // No waiting ChatWindow, check if already processing
                    if (_processingHandles.Contains(codeEditorWindowHandle))
                    {
                        _logger.LogDebug("CodeEditorWindow {Handle} is already being processed", codeEditorWindowHandle);
                        return null;
                    }

                    // Mark as processing
                    _processingHandles.Add(codeEditorWindowHandle);
                    return null; // Indicate that ChatWindow needs to be created
                }

                // Case 4: Register existing ChatWindow with CodeEditorWindow
                if (codeEditorWindowHandle != 0 && chatWindow != null)
                {
                    if (_registrations.TryGetValue(codeEditorWindowHandle, out var existingReg))
                    {
                        if (existingReg == chatWindow)
                        {
                            // Already registered
                            _logger.LogDebug("ChatWindow already registered for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                            return chatWindow;
                        }
                        else
                        {
                            // Different ChatWindow already registered
                            _logger.LogWarning("CodeEditorWindow {Handle} already has a different ChatWindow", codeEditorWindowHandle);
                            return null;
                        }
                    }

                    // Remove from waiting if it was waiting
                    _waitingChatWindows.Remove(chatWindow);
                    
                    // Register new association
                    _registrations[codeEditorWindowHandle] = chatWindow;
                    chatWindow.Closed -= ChatWindow_Closed;
                    chatWindow.Closed += ChatWindow_Closed;
                    
                    _logger.LogDebug("Registered ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                    return chatWindow;
                }

                return null;
            }
        });
    }

    /// <summary>
    /// Handle CodeEditorWindow created - automatically open ChatWindow and attach to CodeEditorWindow
    /// </summary>
    public async Task HandleCodeEditorWindowCreatedAsync(long windowHandle)
    {
        if (windowHandle == 0)
            return;

        // Try to get or create ChatWindow using unified method
        var chatWindow = RegisterOrGetChatWindow(windowHandle, null, false);
        
        if (chatWindow != null)
        {
            // ChatWindow already exists or was found from pending, show at off-screen position before attachment
            // Don't activate to avoid stealing focus from CodeEditorWindow
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                chatWindow.ShowWithPosition(centerOnScreen: false);
            });
            return;
        }

        // Need to create new ChatWindow
        try
        {
            _logger.LogDebug("Creating new ChatWindow for CodeEditorWindow: {Handle}", windowHandle);

            // Wait a bit to see if a waiting ChatWindow registers itself
            // Reduced retry count and delay for faster response
            const int maxRetries = 5; // Reduced from 10 to 5
            const int retryDelayMs = 50; // Reduced from 100ms to 50ms
            for (int retry = 0; retry < maxRetries; retry++)
            {
                await Task.Delay(retryDelayMs);
                
                // Check again if ChatWindow was registered
                chatWindow = RegisterOrGetChatWindow(windowHandle, null, false);
                if (chatWindow != null)
                {
                    _logger.LogDebug("Found ChatWindow after wait for CodeEditorWindow: {Handle}", windowHandle);
                    break;
                }
            }

            if (chatWindow == null)
            {
                // Create new ChatWindow
                chatWindow = GetOrCreateChatWindow(windowHandle);
                if (chatWindow == null)
                {
                    _logger.LogWarning("Failed to create ChatWindow for CodeEditorWindow: {Handle}", windowHandle);
                    return;
                }

                // Register the newly created ChatWindow
                RegisterOrGetChatWindow(windowHandle, chatWindow, false);
            }

            // Verify window handle is valid
            var targetWindowHandle = new IntPtr(windowHandle);
            if (!WindowHelper.IsWindow(targetWindowHandle))
            {
                _logger.LogWarning("Invalid window handle for CodeEditorWindow: {Handle}", windowHandle);
                return;
            }

            // Show and attach ChatWindow to CodeEditorWindow
            // Show at off-screen position to avoid flashing before attachment
            // Use Normal priority for faster response
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                chatWindow.ShowWithPosition(centerOnScreen: false);
            }, System.Windows.Threading.DispatcherPriority.Normal);

            // Set CodeEditor handler ID from window handle (don't wait, do it in parallel)
            var setHandlerTask = chatWindow.ViewModel.SetCodeEditorHandlerIdFromWindowHandleAsync(windowHandle);

            // Attach ChatWindow to CodeEditorWindow immediately (don't wait for handler ID)
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                chatWindow.AttachToWindow(targetWindowHandle, bringToForeground: true);
            }, System.Windows.Threading.DispatcherPriority.Normal);

            // Wait for handler ID to be set (but don't block window showing)
            await setHandlerTask;

            _logger.LogDebug("Successfully opened ChatWindow for CodeEditorWindow: {Handle}", windowHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening ChatWindow for CodeEditorWindow {Handle}", windowHandle);
        }
        finally
        {
            // Always remove processing status when done (success or failure)
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lockObject)
                {
                    _processingHandles.Remove(windowHandle);
                }
            });
        }
    }

    /// <summary>
    /// Get or create a ChatWindow for the specified CodeEditorWindow handle
    /// </summary>
    public ChatWindow? GetOrCreateChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
            return null;

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Check if already registered
                if (_registrations.TryGetValue(codeEditorWindowHandle, out var existing))
                {
                    if (IsWindowValid(existing))
                    {
                        return existing;
                    }
                    else
                    {
                        _registrations.Remove(codeEditorWindowHandle);
                    }
                }

                // Create new ChatWindow
                try
                {
                    var chatWindow = _serviceProvider.GetRequiredService<ChatWindow>();
                    
                    // Register it
                    _registrations[codeEditorWindowHandle] = chatWindow;
                    chatWindow.Closed -= ChatWindow_Closed;
                    chatWindow.Closed += ChatWindow_Closed;
                    
                    // Remove from processing
                    _processingHandles.Remove(codeEditorWindowHandle);
                    
                    _logger.LogDebug("Created new ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                    return chatWindow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create ChatWindow for CodeEditorWindow: {Handle}", codeEditorWindowHandle);
                    _processingHandles.Remove(codeEditorWindowHandle);
                    return null;
                }
            }
        });
    }

    /// <summary>
    /// Get ChatWindow for the specified CodeEditorWindow handle (if exists)
    /// </summary>
    public ChatWindow? GetChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
            return null;

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                if (_registrations.TryGetValue(codeEditorWindowHandle, out var chatWindow))
                {
                    if (IsWindowValid(chatWindow))
                    {
                        return chatWindow;
                    }
                    else
                    {
                        _registrations.Remove(codeEditorWindowHandle);
                    }
                }
                return null;
            }
        });
    }

    /// <summary>
    /// Check if a ChatWindow exists for the specified CodeEditorWindow handle
    /// </summary>
    public bool HasChatWindow(long codeEditorWindowHandle)
    {
        if (codeEditorWindowHandle == 0)
            return false;

        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                if (_registrations.TryGetValue(codeEditorWindowHandle, out var chatWindow))
                {
                    if (IsWindowValid(chatWindow))
                    {
                        return true;
                    }
                    else
                    {
                        _registrations.Remove(codeEditorWindowHandle);
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
                var validWindows = _registrations.Values
                    .Where(IsWindowValid)
                    .ToList();

                // Clean up invalid registrations
                var invalidHandles = _registrations
                    .Where(kvp => !IsWindowValid(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var handle in invalidHandles)
                {
                    _registrations.Remove(handle);
                }

                return validWindows;
            }
        });
    }

    /// <summary>
    /// Get or create a standalone ChatWindow (not attached to any CodeEditorWindow)
    /// </summary>
    public ChatWindow? GetOrCreateStandaloneChatWindow()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Find existing standalone ChatWindow (not in registrations)
                var allWindows = Application.Current.Windows.OfType<ChatWindow>().ToList();
                var standaloneWindow = allWindows.FirstOrDefault(w => 
                    !_registrations.Values.Contains(w) && 
                    !_waitingChatWindows.Contains(w) &&
                    IsWindowValid(w));

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
    /// Simplified: uses unified registration method
    /// </summary>
    public bool RegisterChatWindowForCodeEditor(ChatWindow chatWindow, long codeEditorWindowHandle)
    {
        if (chatWindow == null || codeEditorWindowHandle == 0)
            return false;

        var result = RegisterOrGetChatWindow(codeEditorWindowHandle, chatWindow, false);
        return result == chatWindow;
    }

    /// <summary>
    /// Pre-register a ChatWindow that is about to create a CodeEditorWindow
    /// Simplified: uses unified registration method
    /// </summary>
    public void PreRegisterChatWindow(ChatWindow chatWindow)
    {
        if (chatWindow == null)
            return;

        RegisterOrGetChatWindow(0, chatWindow, true);
    }

    /// <summary>
    /// Complete registration of a pre-registered ChatWindow with actual CodeEditorWindow handle
    /// Simplified: uses unified registration method
    /// </summary>
    public bool CompleteChatWindowRegistration(ChatWindow chatWindow, long codeEditorWindowHandle)
    {
        if (chatWindow == null || codeEditorWindowHandle == 0)
            return false;

        var result = RegisterOrGetChatWindow(codeEditorWindowHandle, chatWindow, false);
        return result == chatWindow;
    }

    /// <summary>
    /// Get an available CodeEditorWindow that doesn't have a ChatWindow yet
    /// </summary>
    public async Task<long> GetAvailableCodeEditorWindowHandleAsync()
    {
        try
        {
            if (!_quickerConnector.IsConnected)
            {
                var connected = await _quickerConnector.WaitConnectAsync(TimeSpan.FromSeconds(5));
                if (!connected)
                {
                    _logger.LogWarning("Quicker service not connected");
                    return 0;
                }
            }

            var handlerId = await _quickerConnector.ServiceClient.GetOrCreateCodeEditorAsync();
            if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
            {
                _logger.LogWarning("Failed to get or create CodeEditorWindow");
                return 0;
            }

            var windowHandle = await _quickerConnector.ServiceClient.GetWindowHandleAsync(handlerId);
            if (windowHandle == 0)
            {
                _logger.LogWarning("Failed to get window handle for CodeEditorWindow handler: {HandlerId}", handlerId);
                return 0;
            }

            if (HasChatWindow(windowHandle))
            {
                _logger.LogDebug("CodeEditorWindow {Handle} already has a ChatWindow", windowHandle);
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
    /// Ensure Quicker service is connected
    /// </summary>
    private async Task<bool> EnsureConnectedAsync()
    {
        if (_quickerConnector.IsConnected)
            return true;

        var connected = await _quickerConnector.WaitConnectAsync(TimeSpan.FromSeconds(5));
        if (!connected)
        {
            _logger.LogWarning("Quicker service not connected");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Get or create a CodeEditorWindow and return handlerId and windowHandle
    /// </summary>
    private async Task<(string? handlerId, long windowHandle)> GetOrCreateCodeEditorWindowAsync()
    {
        string? handlerId;
        long windowHandle;

        // Try to get available CodeEditorWindow first
        var availableWindowHandle = await GetAvailableCodeEditorWindowHandleAsync();
        
        if (availableWindowHandle != 0)
        {
            // Found available CodeEditorWindow, get handler ID
            handlerId = await _quickerConnector.ServiceClient.GetCodeWrapperIdAsync(availableWindowHandle.ToString());
            if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
            {
                _logger.LogWarning("Failed to get handler ID for CodeEditorWindow: {Handle}", availableWindowHandle);
                return (null, 0);
            }
            return (handlerId, availableWindowHandle);
        }

        // No available CodeEditorWindow, create new one
        _logger.LogDebug("Creating new CodeEditorWindow");
        
        handlerId = await _quickerConnector.ServiceClient.GetOrCreateCodeEditorAsync();
        if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
        {
            _logger.LogWarning("Failed to create CodeEditorWindow");
            return (null, 0);
        }

        windowHandle = await _quickerConnector.ServiceClient.GetWindowHandleAsync(handlerId);
        if (windowHandle == 0)
        {
            _logger.LogWarning("Failed to get window handle for CodeEditorWindow handler: {HandlerId}", handlerId);
            return (null, 0);
        }

        return (handlerId, windowHandle);
    }

    /// <summary>
    /// Get or create a CodeEditorWindow for the specified ChatWindow
    /// This method handles the entire process: connection check, pre-registration, getting/creating CodeEditorWindow, and registration
    /// </summary>
    public async Task<(string? handlerId, long windowHandle)> GetOrCreateCodeEditorForChatWindowAsync(ChatWindow chatWindow)
    {
        if (chatWindow == null)
        {
            _logger.LogWarning("ChatWindow is null");
            return (null, 0);
        }

        try
        {
            // Ensure connection
            if (!await EnsureConnectedAsync())
            {
                return (null, 0);
            }

            // Pre-register ChatWindow to prevent duplicate creation
            PreRegisterChatWindow(chatWindow);

            // Get or create CodeEditorWindow
            var (handlerId, windowHandle) = await GetOrCreateCodeEditorWindowAsync();
            if (string.IsNullOrEmpty(handlerId) || windowHandle == 0)
            {
                UnregisterChatWindow(chatWindow);
                return (null, 0);
            }

            // Complete registration
            var registered = CompleteChatWindowRegistration(chatWindow, windowHandle);
            if (!registered)
            {
                _logger.LogWarning("Failed to complete registration for ChatWindow with CodeEditorWindow: {Handle}", windowHandle);
                UnregisterChatWindow(chatWindow);
                return (null, 0);
            }

            _logger.LogDebug("Successfully registered ChatWindow with CodeEditorWindow: {Handle}", windowHandle);
            return (handlerId, windowHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating CodeEditorWindow for ChatWindow");
            UnregisterChatWindow(chatWindow);
            return (null, 0);
        }
    }

    /// <summary>
    /// Unregister a ChatWindow (called when ChatWindow is closed)
    /// This method handles all cleanup: removing from registrations, waiting list, and processing handles
    /// </summary>
    /// <param name="chatWindow">ChatWindow instance to unregister</param>
    public void UnregisterChatWindow(ChatWindow chatWindow)
    {
        if (chatWindow == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            lock (_lockObject)
            {
                // Remove from registrations
                var handleToRemove = _registrations
                    .FirstOrDefault(kvp => kvp.Value == chatWindow).Key;
                if (handleToRemove != 0)
                {
                    _registrations.Remove(handleToRemove);
                    _logger.LogDebug("Unregistered ChatWindow for CodeEditorWindow: {Handle}", handleToRemove);
                }
                
                // Remove from waiting
                _waitingChatWindows.Remove(chatWindow);
                
                // Note: Processing handles are automatically cleaned up when ChatWindow is created
                // or when HandleCodeEditorWindowCreatedAsync completes (in finally block)
            }
        });
    }

    /// <summary>
    /// Handler for ChatWindow Closed event to remove from registrations
    /// </summary>
    private void ChatWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is ChatWindow chatWindow)
        {
            UnregisterChatWindow(chatWindow);
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

            if (!Application.Current.Windows.OfType<ChatWindow>().Contains(window))
                return false;

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
