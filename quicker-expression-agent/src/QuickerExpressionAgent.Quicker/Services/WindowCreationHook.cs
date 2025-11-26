using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Hook to monitor window creation events
/// Raises WindowCreated event when a window is created
/// </summary>
public class WindowCreationHook : IDisposable
{
    private HWINEVENTHOOK _hook;
    private readonly WINEVENTPROC _winEventProc;

    // Windows Event Constants
    private const uint EVENT_OBJECT_CREATE = 0x8000;

    /// <summary>
    /// Event raised when a window is created
    /// </summary>
    public event Action<IntPtr>? WindowCreated;

    public WindowCreationHook()
    {
        _winEventProc = new WINEVENTPROC(WinEventProc);
    }

    /// <summary>
    /// Start the hook to monitor window creation events
    /// </summary>
    public void StartHook()
    {
        // Check if hook is already set
        if (_hook.Value != IntPtr.Zero)
        {
            return; // Already started
        }

        // Set hook for system-wide window creation events
        // Flags: 0 = WINEVENT_OUTOFCONTEXT (hook runs in separate thread)
        // Note: WINEVENT_OUTOFCONTEXT means the hook callback runs in a separate thread
        // but it still requires the thread to have a message loop or proper thread context
        _hook = SetWinEventHook(
            EVENT_OBJECT_CREATE,  // Min event: window creation
            EVENT_OBJECT_CREATE,  // Max event: window creation
            new HINSTANCE(IntPtr.Zero),
            _winEventProc,
            0,  // processId: 0 = all processes
            0,  // threadId: 0 = all threads
            0); // flags: 0 = WINEVENT_OUTOFCONTEXT

        if (_hook.Value == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to set WinEvent hook for window creation monitoring");
        }
    }

    private void WinEventProc(
        HWINEVENTHOOK hWinEventHook,
        uint @event,
        HWND hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        // CRITICAL: Hook callbacks must return as quickly as possible
        // Any blocking operations here will cause UI freezing
        
        // Only process EVENT_OBJECT_CREATE events
        if (@event != EVENT_OBJECT_CREATE)
            return;

        // Validate window handle (fast check)
        if (hwnd.Value == IntPtr.Zero)
            return;

        // Only process window-level events (not child elements)
        // OBJID_WINDOW = 0x00000000
        // Check this BEFORE IsWindow to avoid unnecessary API call
        if (idObject != 0 || idChild != 0)
            return;

        // Verify it's a valid window (not just any handle)
        // Note: IsWindow can be slow, but we need it to filter out invalid handles
        // We check idObject/idChild first to reduce calls to IsWindow
        if (!IsWindow(hwnd))
            return;

        // EVENT_OBJECT_CREATE fires when a window is created
        // hwnd is the window that was created
        // HandleWindowCreated should be fast and not block
        HandleWindowCreated(hwnd);
    }

    /// <summary>
    /// Handle window creation event
    /// </summary>
    private void HandleWindowCreated(HWND hwnd)
    {
        // Raise event when a window is created
        // Note: This callback runs in a separate thread (WINEVENT_OUTOFCONTEXT)
        // The callback thread is managed by Windows, but it may not have a message loop
        // Subscribers should handle thread marshaling if needed
        try
        {
            // Check if there are any subscribers
            if (WindowCreated == null)
            {
                // No subscribers - this shouldn't happen but log it
                System.Diagnostics.Debug.WriteLine("WindowCreated event has no subscribers");
                return;
            }

            // Invoke event - subscribers should handle thread marshaling if needed
            WindowCreated.Invoke(hwnd.Value);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we're in a hook callback
            // If exception occurs, it might prevent future callbacks from working
            // Try to log to file or use other mechanism
            try
            {
                // Try to write to a log file as fallback
                var logPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "WindowCreationHook_Errors.log");
                System.IO.File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error in HandleWindowCreated: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch
            {
                // If file logging fails, at least try Debug output
                System.Diagnostics.Debug.WriteLine($"Error in HandleWindowCreated: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_hook.Value != IntPtr.Zero)
        {
            try
            {
                UnhookWinEvent(_hook);
                _hook = default;
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
        GC.SuppressFinalize(this);
    }
}

