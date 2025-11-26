using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Hook to monitor active window (foreground window) changes
/// Raises ActiveWindowChanged event when foreground window changes
/// </summary>
public class ActiveWindowHook : IDisposable
{
    private HWINEVENTHOOK _hook;
    private readonly WINEVENTPROC _winEventProc;

    // Windows Event Constants
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

    /// <summary>
    /// Event raised when the active (foreground) window changes
    /// </summary>
    public event Action<IntPtr>? ActiveWindowChanged;

    public ActiveWindowHook()
    {
        _winEventProc = new WINEVENTPROC(WinEventProc);
    }

    /// <summary>
    /// Start the hook to monitor foreground window changes
    /// </summary>
    public void StartHook()
    {
        // Check if hook is already set
        if (_hook.Value != IntPtr.Zero)
        {
            return; // Already started
        }

        // Set hook for system-wide foreground window events
        // Flags: 0 = WINEVENT_OUTOFCONTEXT (hook runs in separate thread)
        // Note: WINEVENT_OUTOFCONTEXT means the hook callback runs in a separate thread
        // but it still requires the thread to have a message loop or proper thread context
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,  // Min event: foreground window change
            EVENT_SYSTEM_FOREGROUND,  // Max event: foreground window change
            new HINSTANCE(IntPtr.Zero),
            _winEventProc,
            0,  // processId: 0 = all processes
            0,  // threadId: 0 = all threads
            0); // flags: 0 = WINEVENT_OUTOFCONTEXT

        if (_hook.Value == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to set WinEvent hook for foreground monitoring");
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
        // Only process EVENT_SYSTEM_FOREGROUND events
        if (@event != EVENT_SYSTEM_FOREGROUND)
            return;

        // Validate window handle
        if (hwnd.Value == IntPtr.Zero)
            return;

        // Verify it's a valid window (not just any handle)
        if (!IsWindow(hwnd))
            return;

        // Only process window-level events (not child elements)
        if (idObject != 0 || idChild != 0)
            return;

        // EVENT_SYSTEM_FOREGROUND fires when a window becomes foreground
        // hwnd is the window that became foreground
        HandleForegroundWindowChange(hwnd);
    }

    /// <summary>
    /// Handle foreground window change event
    /// </summary>
    private void HandleForegroundWindowChange(HWND hwnd)
    {
        // Raise event when foreground window changes
        // Note: This callback runs in a separate thread (WINEVENT_OUTOFCONTEXT)
        // The callback thread is managed by Windows, but it may not have a message loop
        // Subscribers should handle thread marshaling if needed
        try
        {
            // Check if there are any subscribers
            if (ActiveWindowChanged == null)
            {
                // No subscribers - this shouldn't happen but log it
                System.Diagnostics.Debug.WriteLine("ActiveWindowChanged event has no subscribers");
                return;
            }

            // Invoke event - subscribers should handle thread marshaling if needed
            ActiveWindowChanged.Invoke(hwnd.Value);
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
                    "ActiveWindowHook_Errors.log");
                System.IO.File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error in HandleForegroundWindowChange: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch
            {
                // If file logging fails, at least try Debug output
                System.Diagnostics.Debug.WriteLine($"Error in HandleForegroundWindowChange: {ex.Message}");
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

