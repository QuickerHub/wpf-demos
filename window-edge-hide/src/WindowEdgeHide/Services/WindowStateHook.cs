using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Hook to monitor window state changes (minimize, maximize, visibility, activation)
    /// Also monitors mouse capture events to detect user interaction (e.g., dragging) for better activation detection
    /// </summary>
    internal class WindowStateHook : IDisposable
    {
        private HWINEVENTHOOK _hook;
        private readonly WINEVENTPROC _winEventProc;
        private readonly HWND _targetWindowHandle;
        private bool _wasForeground;

        // Windows Event Constants
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint EVENT_SYSTEM_CAPTURESTART = 0x0008;
        private const uint EVENT_SYSTEM_CAPTUREEND = 0x0009;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_SHOW = 0x8002;

        // Array of events to monitor
        private static readonly uint[] EventsToMonitor = new[]
        {
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_CAPTURESTART,
            EVENT_SYSTEM_CAPTUREEND,
            EVENT_SYSTEM_MINIMIZESTART,
            EVENT_SYSTEM_MINIMIZEEND,
            EVENT_OBJECT_LOCATIONCHANGE,
            EVENT_OBJECT_HIDE,
            EVENT_OBJECT_SHOW
        };

        /// <summary>
        /// Event raised when window state changes (minimize, maximize, restore)
        /// </summary>
        public event Action<IntPtr>? StateChanged;

        /// <summary>
        /// Event raised when window visibility changes
        /// </summary>
        public event Action<IntPtr, bool>? VisibilityChanged;

        /// <summary>
        /// Event raised when window is activated (brought to foreground)
        /// </summary>
        public event Action<IntPtr>? Activated;

        /// <summary>
        /// Event raised when window is deactivated (loses foreground)
        /// </summary>
        public event Action<IntPtr>? Deactivated;

        /// <summary>
        /// Create a hook to monitor window state events
        /// </summary>
        /// <param name="targetWindowHandle">Handle of the window to monitor</param>
        public WindowStateHook(IntPtr targetWindowHandle)
        {
            _targetWindowHandle = new HWND(targetWindowHandle);
            _winEventProc = new WINEVENTPROC(WinEventProc);
            
            // Check initial activation state
            var currentForeground = GetForegroundWindow();
            _wasForeground = currentForeground == _targetWindowHandle;
        }

        /// <summary>
        /// Start the hook
        /// </summary>
        public void StartHook()
        {
            // Calculate min and max event IDs from the events array
            uint minEvent = EventsToMonitor[0];
            uint maxEvent = EventsToMonitor[0];
            
            foreach (uint eventId in EventsToMonitor)
            {
                if (eventId < minEvent)
                    minEvent = eventId;
                if (eventId > maxEvent)
                    maxEvent = eventId;
            }

            // Hook multiple events: foreground, mouse capture start/end, minimize start/end, location change (for maximize/restore), show/hide
            _hook = SetWinEventHook(
                minEvent,
                maxEvent,
                new HINSTANCE(IntPtr.Zero),
                _winEventProc,
                0,
                0,
                0);
        }

        private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            switch (@event)
            {
                case EVENT_SYSTEM_FOREGROUND:
                    // Foreground window changed - check if it's our target window
                    if (hwnd == _targetWindowHandle)
                    {
                        // Target window became foreground
                        _wasForeground = true;
                        Activated?.Invoke(_targetWindowHandle.Value);
                    }
                    else
                    {
                        // Another window became foreground
                        // Verify by checking current foreground window to handle edge cases
                        // This is important for topmost windows that might not be the true foreground window
                        var currentForeground = GetForegroundWindow();
                        bool isTargetForeground = currentForeground == _targetWindowHandle;
                        
                        if (isTargetForeground)
                        {
                            // Target window is actually foreground (edge case: event might fire out of order)
                            _wasForeground = true;
                            Activated?.Invoke(_targetWindowHandle.Value);
                        }
                        else if (_wasForeground)
                        {
                            // Target window was previously foreground and now lost focus
                            _wasForeground = false;
                            Deactivated?.Invoke(_targetWindowHandle.Value);
                        }
                        // If _wasForeground is false and target is not foreground, do nothing
                        // (target window was never foreground, so no need to trigger deactivated)
                    }
                    break;

                case EVENT_SYSTEM_CAPTURESTART:
                    // Mouse capture started - check if it's our target window
                    // This indicates user interaction with the window (e.g., dragging)
                    if (hwnd == _targetWindowHandle)
                    {
                        // Window started capturing mouse, consider it active
                        if (!_wasForeground)
                        {
                            _wasForeground = true;
                            Activated?.Invoke(_targetWindowHandle.Value);
                        }
                    }
                    break;

                case EVENT_SYSTEM_CAPTUREEND:
                    // Mouse capture ended - check if it's our target window
                    if (hwnd == _targetWindowHandle)
                    {
                        // Window released mouse capture, check if still foreground
                        var currentForeground = GetForegroundWindow();
                        if (currentForeground != _targetWindowHandle && _wasForeground)
                        {
                            _wasForeground = false;
                            Deactivated?.Invoke(_targetWindowHandle.Value);
                        }
                    }
                    break;

                case EVENT_SYSTEM_MINIMIZESTART:
                case EVENT_SYSTEM_MINIMIZEEND:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        // Window minimize state changed
                        StateChanged?.Invoke(_targetWindowHandle.Value);
                    }
                    break;

                case EVENT_OBJECT_LOCATIONCHANGE:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        // Window position or size changed (could be maximize/restore)
                        StateChanged?.Invoke(_targetWindowHandle.Value);
                    }
                    break;

                case EVENT_OBJECT_HIDE:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        // Window was hidden
                        VisibilityChanged?.Invoke(_targetWindowHandle.Value, false);
                    }
                    break;

                case EVENT_OBJECT_SHOW:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        // Window was shown
                        VisibilityChanged?.Invoke(_targetWindowHandle.Value, true);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            if (_hook.Value != IntPtr.Zero)
            {
                UnhookWinEvent(_hook);
                _hook = default;
            }
            GC.SuppressFinalize(this);
        }
    }
}

