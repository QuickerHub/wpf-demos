using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowAttach.Models;
using WINDOW_EX_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using WINDOW_LONG_PTR_INDEX = Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX;
using SET_WINDOW_POS_FLAGS = Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for window operations using Win32 API
    /// </summary>
    public static class WindowHelper
    {
        // DllImport declarations for GetWindowLongPtr and SetWindowLongPtr
        // These are macros in Win32 that map to GetWindowLong/SetWindowLong on 64-bit systems
        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);
        /// <summary>
        /// Validate window handle and return HWND, or null if invalid
        /// </summary>
        private static HWND? ValidateWindowHandle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            var hwnd = new HWND(hWnd);
            if (!Windows.Win32.PInvoke.IsWindow(hwnd))
                return null;

            return hwnd;
        }

        /// <summary>
        /// Get window long pointer value with validation
        /// </summary>
        private static nint? GetWindowLongPtrValue(IntPtr hWnd, WINDOW_LONG_PTR_INDEX index)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return null;

            return GetWindowLongPtr(hwnd.Value.Value, (int)index);
        }

        /// <summary>
        /// Get window long pointer value from validated HWND (no validation)
        /// </summary>
        private static nint GetWindowLongPtrValue(HWND hwnd, WINDOW_LONG_PTR_INDEX index)
        {
            return GetWindowLongPtr(hwnd.Value, (int)index);
        }

        /// <summary>
        /// Set window long pointer value with bitwise operations
        /// </summary>
        private static void SetWindowLongPtrFlags(HWND hwnd, WINDOW_LONG_PTR_INDEX index, uint flags, bool value)
        {
            var currentValue = (uint)GetWindowLongPtrValue(hwnd, index);
            var newValue = value ? (currentValue | flags) : (currentValue & ~flags);
            // Use SetWindowLongPtr to handle both 32-bit and 64-bit values correctly
            SetWindowLongPtr(hwnd.Value, (int)index, (nint)newValue);
        }

        /// <summary>
        /// Get window rectangle
        /// </summary>
        public static WindowRect? GetWindowRect(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return null;

            Windows.Win32.PInvoke.GetWindowRect(hwnd.Value, out RECT rect);
            return new WindowRect(rect.left, rect.top, rect.right, rect.bottom);
        }

        /// <summary>
        /// Set window position
        /// </summary>
        internal static bool SetWindowPos(IntPtr hWnd, int x, int y, int width, int height, SET_WINDOW_POS_FLAGS flags)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            return Windows.Win32.PInvoke.SetWindowPos(hwnd.Value, HWND.Null, x, y, width, height, flags);
        }

        /// <summary>
        /// Get monitor work area for a window
        /// </summary>
        public static WindowRect? GetMonitorWorkArea(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return null;

            var monitor = MonitorFromWindow(hwnd.Value, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitor.Value == IntPtr.Zero)
                return null;

            var monitorInfo = new MONITORINFO
            {
                cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
            };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return null;

            var rcWork = monitorInfo.rcWork;
            return new WindowRect(rcWork.left, rcWork.top, rcWork.right, rcWork.bottom);
        }

        /// <summary>
        /// Check if window has a specific extended style flag
        /// </summary>
        private static bool HasWindowExStyleFlag(IntPtr hWnd, WINDOW_EX_STYLE flag)
        {
            var longPtr = GetWindowLongPtrValue(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if (!longPtr.HasValue)
                return false;

            var exStyle = (WINDOW_EX_STYLE)longPtr.Value;
            return exStyle.HasFlag(flag);
        }

        /// <summary>
        /// Check if a window handle is valid
        /// </summary>
        /// <param name="hWnd">Window handle to check</param>
        /// <returns>True if the handle is valid, false otherwise</returns>
        public static bool IsWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            return Windows.Win32.PInvoke.IsWindow(hwnd);
        }

        /// <summary>
        /// Check if window has WS_EX_TOOLWINDOW extended style (not shown in taskbar)
        /// </summary>
        public static bool IsToolWindow(IntPtr hWnd)
        {
            return HasWindowExStyleFlag(hWnd, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
        }

        /// <summary>
        /// Check if window has WS_EX_NOACTIVATE extended style (does not activate when clicked)
        /// </summary>
        public static bool IsNoActivateWindow(IntPtr hWnd)
        {
            return HasWindowExStyleFlag(hWnd, WINDOW_EX_STYLE.WS_EX_NOACTIVATE);
        }

        /// <summary>
        /// Set window extended style (e.g., WS_EX_NOACTIVATE)
        /// </summary>
        internal static bool SetWindowExStyle(IntPtr hWnd, WINDOW_EX_STYLE exStyle, bool value)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            SetWindowLongPtrFlags(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (uint)exStyle, value);
            return true;
        }

        /// <summary>
        /// Set window z-order relative to another window
        /// </summary>
        internal static bool SetWindowZOrder(IntPtr hWnd, IntPtr hWndInsertAfter, SET_WINDOW_POS_FLAGS flags)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            HWND hwndAfter;
            if (hWndInsertAfter == IntPtr.Zero)
            {
                hwndAfter = HWND.Null; // HWND_TOP
            }
            else if (hWndInsertAfter == new IntPtr(-1))
            {
                hwndAfter = new HWND(new IntPtr(-1)); // HWND_TOPMOST
            }
            else if (hWndInsertAfter == new IntPtr(-2))
            {
                hwndAfter = new HWND(new IntPtr(-2)); // HWND_NOTOPMOST
            }
            else
            {
                hwndAfter = new HWND(hWndInsertAfter);
            }
            
            return Windows.Win32.PInvoke.SetWindowPos(hwnd.Value, hwndAfter, 0, 0, 0, 0, 
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | flags);
        }

        /// <summary>
        /// Set window owner (GWLP_HWNDPARENT) to make it follow the owner window's virtual desktop
        /// </summary>
        public static bool SetWindowOwner(IntPtr hWnd, IntPtr hWndOwner, bool preventActivation = true)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            // Use SetWindowLongPtr for pointer values (GWLP_* indexes) to handle 64-bit pointers correctly
            var previousOwner = SetWindowLongPtr(hwnd.Value.Value, (int)WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, hWndOwner);
            
            if (preventActivation)
            {
                bool setNoActivate = hWndOwner != IntPtr.Zero;
                SetWindowLongPtrFlags(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (uint)WINDOW_EX_STYLE.WS_EX_NOACTIVATE, setNoActivate);
            }
            
            return previousOwner != IntPtr.Zero || hWndOwner == IntPtr.Zero;
        }

        /// <summary>
        /// Bring window to foreground and activate it
        /// </summary>
        /// <param name="hWnd">Window handle to activate</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool BringWindowToForeground(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            try
            {
                // First, restore window if minimized
                if (IsIconic(hwnd.Value))
                {
                    ShowWindow(hwnd.Value, SHOW_WINDOW_CMD.SW_RESTORE);
                }

                // Bring window to foreground
                return SetForegroundWindow(hwnd.Value);
            }
            catch
            {
                return false;
            }
        }
    }
}

