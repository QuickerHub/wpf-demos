using System;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace WindowAttach.Services
{
    /// <summary>
    /// Unified hook to monitor multiple window events
    /// Provides events similar to WPF Window class (LocationChanged, Destroyed, etc.)
    /// </summary>
    public class WindowEventHook : IDisposable
    {
        private HWINEVENTHOOK _hook;
        private readonly WINEVENTPROC _winEventProc;
        private readonly HWND _targetWindowHandle;

        // Windows Event Constants
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        // Array of events to monitor
        private static readonly uint[] EventsToMonitor = new[]
        {
            EVENT_OBJECT_LOCATIONCHANGE,
            EVENT_OBJECT_DESTROY,
            EVENT_SYSTEM_MINIMIZESTART,
            EVENT_SYSTEM_MINIMIZEEND,
            EVENT_SYSTEM_FOREGROUND
        };

        private bool _lastVisibilityState = true; // Track last visibility state

        /// <summary>
        /// Event raised when window position or size changes
        /// </summary>
        public event Action<IntPtr>? LocationChanged;

        /// <summary>
        /// Event raised when window is destroyed
        /// </summary>
        public event Action<IntPtr>? Destroyed;

        /// <summary>
        /// Event raised when window visibility changes (hidden or minimized)
        /// </summary>
        public event Action<IntPtr, bool>? VisibilityChanged;

        /// <summary>
        /// Event raised when window is activated (brought to foreground)
        /// </summary>
        public event Action<IntPtr>? Activated;

        /// <summary>
        /// Create a hook to monitor window events
        /// </summary>
        /// <param name="targetWindowHandle">Handle of the window to monitor</param>
        public WindowEventHook(IntPtr targetWindowHandle)
        {
            _targetWindowHandle = new HWND(targetWindowHandle);
            _winEventProc = new WINEVENTPROC(WinEventProc);
        }

        /// <summary>
        /// Start the hook
        /// </summary>
        public void StartHook()
        {
            // Calculate min and max from the events array
            uint eventMin = EventsToMonitor.Min();
            uint eventMax = EventsToMonitor.Max();
            
            // Initialize visibility state
            CheckAndNotifyVisibility();
            
            // Flags: 0 = WINEVENT_OUTOFCONTEXT (hook runs in separate thread)
            _hook = SetWinEventHook(
                eventMin,  // Min event: calculated minimum
                eventMax,  // Max event: calculated maximum
                new HINSTANCE(IntPtr.Zero),
                _winEventProc,
                0,
                0,
                0);
        }

        /// <summary>
        /// Check window visibility state and notify if changed
        /// </summary>
        private void CheckAndNotifyVisibility()
        {
            var hwnd = _targetWindowHandle;
            if (hwnd.Value == IntPtr.Zero)
                return;

            // Check if window is visible and not minimized (both minimized and hidden count as hidden)
            bool isVisible = IsWindowVisible(hwnd) && !IsIconic(hwnd);
            
            if (isVisible != _lastVisibilityState)
            {
                _lastVisibilityState = isVisible;
                VisibilityChanged?.Invoke(hwnd.Value, isVisible);
            }
        }

        private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            // Dispatch events based on event type
            switch (@event)
            {
                case EVENT_OBJECT_LOCATIONCHANGE:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        LocationChanged?.Invoke(_targetWindowHandle.Value);
                        // Also check visibility on location change (window might be minimized/maximized)
                        CheckAndNotifyVisibility();
                    }
                    break;

                case EVENT_OBJECT_DESTROY:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        Destroyed?.Invoke(_targetWindowHandle.Value);
                    }
                    break;

                case EVENT_SYSTEM_MINIMIZESTART:
                case EVENT_SYSTEM_MINIMIZEEND:
                    // Only process events for the target window
                    if (hwnd == _targetWindowHandle && idObject == 0 && idChild == 0)
                    {
                        // Check visibility when minimize state changes
                        CheckAndNotifyVisibility();
                    }
                    break;

                case EVENT_SYSTEM_FOREGROUND:
                    // EVENT_SYSTEM_FOREGROUND fires for all windows, check if it's our target window
                    // For foreground events, hwnd is the window that became foreground
                    if (hwnd == _targetWindowHandle)
                    {
                        Activated?.Invoke(_targetWindowHandle.Value);
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

