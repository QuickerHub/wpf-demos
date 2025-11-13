using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using static Windows.Win32.PInvoke;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Hook to monitor window activation events
    /// When window is activated, raises Activated event
    /// When window is deactivated, raises Deactivated event
    /// </summary>
    internal class WindowActivationHook : IDisposable
    {
        private HWINEVENTHOOK _hook;
        private readonly WINEVENTPROC _winEventProc;
        private readonly HWND _targetWindowHandle;
        private bool _wasForeground;

        // Windows Event Constants
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

        /// <summary>
        /// Event raised when window is activated (brought to foreground)
        /// </summary>
        public event Action<IntPtr>? Activated;

        /// <summary>
        /// Event raised when window is deactivated (loses foreground)
        /// </summary>
        public event Action<IntPtr>? Deactivated;

        /// <summary>
        /// Create a hook to monitor window activation events
        /// </summary>
        /// <param name="targetWindowHandle">Handle of the window to monitor</param>
        public WindowActivationHook(IntPtr targetWindowHandle)
        {
            _targetWindowHandle = new HWND(targetWindowHandle);
            _winEventProc = new WINEVENTPROC(WinEventProc);
            
            // Check initial state
            var currentForeground = GetForegroundWindow();
            _wasForeground = currentForeground == _targetWindowHandle;
        }

        /// <summary>
        /// Start the hook
        /// </summary>
        public void StartHook()
        {
            // Flags: 0 = WINEVENT_OUTOFCONTEXT (hook runs in separate thread)
            _hook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                new HINSTANCE(IntPtr.Zero),
                _winEventProc,
                0,
                0,
                0);
        }

        private void WinEventProc(HWINEVENTHOOK hWinEventHook, uint @event, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            // EVENT_SYSTEM_FOREGROUND fires for all windows
            // For foreground events, hwnd is the window that became foreground
            if (@event == EVENT_SYSTEM_FOREGROUND)
            {
                if (hwnd == _targetWindowHandle)
                {
                    // Target window became foreground
                    _wasForeground = true;
                    Activated?.Invoke(_targetWindowHandle.Value);
                }
                else if (_wasForeground)
                {
                    // Another window became foreground, target window lost focus
                    _wasForeground = false;
                    Deactivated?.Invoke(_targetWindowHandle.Value);
                }
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

