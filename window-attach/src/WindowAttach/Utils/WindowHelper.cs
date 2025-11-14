using System;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowAttach.Models;
using System.Runtime.InteropServices;
using WINDOW_EX_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using WINDOW_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE;
using WINDOW_LONG_PTR_INDEX = Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX;
using SET_WINDOW_POS_FLAGS = Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for window operations using Win32 API
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// Validate window handle and return HWND, or null if invalid
        /// </summary>
        private static HWND? ValidateWindowHandle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
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

            return Windows.Win32.PInvoke.GetWindowLongPtr(hwnd.Value, index);
        }

        /// <summary>
        /// Get window long pointer value from validated HWND (no validation)
        /// </summary>
        private static nint GetWindowLongPtrValue(HWND hwnd, WINDOW_LONG_PTR_INDEX index)
        {
            return Windows.Win32.PInvoke.GetWindowLongPtr(hwnd, index);
        }

        /// <summary>
        /// Set window long pointer value with bitwise operations
        /// </summary>
        /// <param name="hwnd">Validated window handle</param>
        /// <param name="index">Window long pointer index</param>
        /// <param name="flags">Flags to set or clear</param>
        /// <param name="value">True to set flags, false to clear flags</param>
        private static void SetWindowLongPtrFlags(HWND hwnd, WINDOW_LONG_PTR_INDEX index, uint flags, bool value)
        {
            var currentValue = (uint)GetWindowLongPtrValue(hwnd, index);
            var newValue = value ? (currentValue | flags) : (currentValue & ~flags);
            Windows.Win32.PInvoke.SetWindowLongPtr(hwnd, index, (nint)newValue);
        }

        /// <summary>
        /// Get window rectangle
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Window rectangle, or null if failed</returns>
        public static WindowRect? GetWindowRect(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return null;

            // Use fully qualified name to avoid conflict with this method
            Windows.Win32.PInvoke.GetWindowRect(hwnd.Value, out RECT rect);
            return new WindowRect(rect.left, rect.top, rect.right, rect.bottom);
        }

        /// <summary>
        /// Set window position
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <param name="flags">Window position flags</param>
        /// <returns>True if successful</returns>
        internal static bool SetWindowPos(IntPtr hWnd, int x, int y, int width, int height, SET_WINDOW_POS_FLAGS flags)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            // Use fully qualified name to avoid conflict
            return Windows.Win32.PInvoke.SetWindowPos(hwnd.Value, HWND.Null, x, y, width, height, flags);
        }

        /// <summary>
        /// Get monitor work area for a window
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Monitor work area rectangle, or null if failed</returns>
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
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>()
            };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return null;

            var rcWork = monitorInfo.rcWork;
            return new WindowRect(rcWork.left, rcWork.top, rcWork.right, rcWork.bottom);
        }

        /// <summary>
        /// Get window title text
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Window title, or empty string if failed</returns>
        public static string GetWindowText(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return string.Empty;

            int length = Windows.Win32.PInvoke.GetWindowTextLength(hwnd.Value);
            if (length == 0)
                return string.Empty;

            unsafe
            {
                var buffer = new char[length + 1];
                fixed (char* pBuffer = buffer)
                {
                    Windows.Win32.PInvoke.GetWindowText(hwnd.Value, new Windows.Win32.Foundation.PWSTR(pBuffer), length + 1);
                    return new string(buffer, 0, length);
                }
            }
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
        /// Check if window has WS_EX_TOOLWINDOW extended style (not shown in taskbar)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>True if window has WS_EX_TOOLWINDOW style</returns>
        public static bool IsToolWindow(IntPtr hWnd)
        {
            return HasWindowExStyleFlag(hWnd, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
        }

        /// <summary>
        /// Check if window has WS_EX_NOACTIVATE extended style (does not activate when clicked)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>True if window has WS_EX_NOACTIVATE style</returns>
        public static bool IsNoActivateWindow(IntPtr hWnd)
        {
            return HasWindowExStyleFlag(hWnd, WINDOW_EX_STYLE.WS_EX_NOACTIVATE);
        }

        /// <summary>
        /// Set window extended style (e.g., WS_EX_NOACTIVATE)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="exStyle">Extended style to set</param>
        /// <param name="value">True to add style, false to remove</param>
        /// <returns>True if successful</returns>
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
        /// <param name="hWnd">Window handle to set z-order</param>
        /// <param name="hWndInsertAfter">Window handle to insert after (use IntPtr.Zero for HWND_TOP, IntPtr(-1) for HWND_TOPMOST, IntPtr(-2) for HWND_NOTOPMOST, or another window handle)</param>
        /// <param name="flags">Window position flags</param>
        /// <returns>True if successful</returns>
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
                // Insert after the specified window (same z-order level)
                hwndAfter = new HWND(hWndInsertAfter);
            }
            
            // Use SWP_NOMOVE | SWP_NOSIZE to only change z-order
            return Windows.Win32.PInvoke.SetWindowPos(hwnd.Value, hwndAfter, 0, 0, 0, 0, 
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | flags);
        }

        /// <summary>
        /// Set window owner (GWLP_HWNDPARENT) to make it follow the owner window's virtual desktop
        /// This is different from SetParent - it sets the window owner without making it a child window
        /// Also sets WS_EX_NOACTIVATE to prevent the window from getting focus when clicked
        /// </summary>
        /// <param name="hWnd">Window handle (popup window)</param>
        /// <param name="hWndOwner">Owner window handle (window2)</param>
        /// <param name="preventActivation">If true, prevent window from getting focus when clicked (default: true)</param>
        /// <returns>True if successful</returns>
        public static bool SetWindowOwner(IntPtr hWnd, IntPtr hWndOwner, bool preventActivation = true)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return false;

            // Set GWLP_HWNDPARENT to make the window follow the owner's virtual desktop
            // This is safer than SetParent for WPF windows as it doesn't create a parent-child relationship
            var previousOwner = Windows.Win32.PInvoke.SetWindowLongPtr(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, hWndOwner);
            
            // If setting owner (not clearing), also set WS_EX_NOACTIVATE to prevent focus stealing
            // Use validated HWND directly to avoid re-validation
            if (preventActivation)
            {
                bool setNoActivate = hWndOwner != IntPtr.Zero;
                SetWindowLongPtrFlags(hwnd.Value, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (uint)WINDOW_EX_STYLE.WS_EX_NOACTIVATE, setNoActivate);
            }
            
            return previousOwner != IntPtr.Zero || hWndOwner == IntPtr.Zero;
        }

        /// <summary>
        /// Get window title (alias for GetWindowText)
        /// </summary>
        public static string? GetWindowTitle(IntPtr hWnd)
        {
            var title = GetWindowText(hWnd);
            return string.IsNullOrEmpty(title) ? null : title;
        }

        /// <summary>
        /// Get window extended style
        /// </summary>
        internal static WINDOW_EX_STYLE GetWindowExStyle(IntPtr hWnd)
        {
            var longPtr = GetWindowLongPtrValue(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            return longPtr.HasValue ? (WINDOW_EX_STYLE)longPtr.Value : (WINDOW_EX_STYLE)0;
        }

        /// <summary>
        /// Get window style
        /// </summary>
        internal static WINDOW_STYLE GetWindowStyle(IntPtr hWnd)
        {
            var longPtr = GetWindowLongPtrValue(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            return longPtr.HasValue ? (WINDOW_STYLE)longPtr.Value : (WINDOW_STYLE)0;
        }

        /// <summary>
        /// Get window owner (GWLP_HWNDPARENT)
        /// </summary>
        public static IntPtr GetWindowOwner(IntPtr hWnd)
        {
            var longPtr = GetWindowLongPtrValue(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT);
            return longPtr ?? IntPtr.Zero;
        }

        /// <summary>
        /// Get window parent
        /// </summary>
        public static IntPtr GetWindowParent(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return IntPtr.Zero;

            return Windows.Win32.PInvoke.GetParent(hwnd.Value).Value;
        }

        /// <summary>
        /// Get window process ID
        /// </summary>
        public static uint GetWindowProcessId(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return 0;

            unsafe
            {
                uint processId = 0;
                Windows.Win32.PInvoke.GetWindowThreadProcessId(hwnd.Value, &processId);
                return processId;
            }
        }

        /// <summary>
        /// Get window thread ID
        /// </summary>
        public static uint GetWindowThreadId(IntPtr hWnd)
        {
            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return 0;

            unsafe
            {
                uint processId = 0;
                return Windows.Win32.PInvoke.GetWindowThreadProcessId(hwnd.Value, &processId);
            }
        }

        /// <summary>
        /// Get window topmost state
        /// </summary>
        public static bool GetWindowTopmost(IntPtr hWnd)
        {
            return HasWindowExStyleFlag(hWnd, WINDOW_EX_STYLE.WS_EX_TOPMOST);
        }

        /// <summary>
        /// Get root window handle for a given window handle
        /// If the input handle is a child window, returns the root window handle
        /// </summary>
        /// <param name="hWnd">Window handle (can be a child window)</param>
        /// <returns>Root window handle, or original handle if already root or failed</returns>
        public static IntPtr GetRootWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;

            var hwnd = ValidateWindowHandle(hWnd);
            if (!hwnd.HasValue)
                return IntPtr.Zero;

            try
            {
                // Use GetAncestor with GA_ROOT to get the root window
                var rootHwnd = Windows.Win32.PInvoke.GetAncestor(hwnd.Value, GET_ANCESTOR_FLAGS.GA_ROOT);
                
                // If GetAncestor returns null or desktop window, return the original handle
                if (rootHwnd.Value == IntPtr.Zero)
                    return hWnd;

                // Check if it's the desktop window
                var desktopHwnd = Windows.Win32.PInvoke.GetDesktopWindow();
                if (rootHwnd.Value == desktopHwnd.Value)
                    return hWnd;

                return rootHwnd.Value;
            }
            catch
            {
                // If GetAncestor fails, return original handle
                return hWnd;
            }
        }
    }
}

