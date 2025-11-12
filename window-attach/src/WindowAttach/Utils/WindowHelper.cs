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
using WINDOW_LONG_PTR_INDEX = Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for window operations using Win32 API
    /// </summary>
    internal static class WindowHelper
    {

        /// <summary>
        /// Get window rectangle
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Window rectangle, or null if failed</returns>
        internal static WindowRect? GetWindowRect(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return null;

            // Use fully qualified name to avoid conflict with this method
            Windows.Win32.PInvoke.GetWindowRect(hwnd, out RECT rect);
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
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            // Use fully qualified name to avoid conflict
            return Windows.Win32.PInvoke.SetWindowPos(hwnd, HWND.Null, x, y, width, height, flags);
        }

        /// <summary>
        /// Get monitor work area for a window
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Monitor work area rectangle, or null if failed</returns>
        internal static WindowRect? GetMonitorWorkArea(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return null;

            var monitor = MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
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
        internal static string GetWindowText(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return string.Empty;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return string.Empty;

            int length = Windows.Win32.PInvoke.GetWindowTextLength(hwnd);
            if (length == 0)
                return string.Empty;

            unsafe
            {
                var buffer = new char[length + 1];
                fixed (char* pBuffer = buffer)
                {
                    Windows.Win32.PInvoke.GetWindowText(hwnd, new Windows.Win32.Foundation.PWSTR(pBuffer), length + 1);
                    return new string(buffer, 0, length);
                }
            }
        }

        /// <summary>
        /// Check if window has WS_EX_TOOLWINDOW extended style (not shown in taskbar)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>True if window has WS_EX_TOOLWINDOW style</returns>
        internal static bool IsToolWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            var exStyle = (WINDOW_EX_STYLE)Windows.Win32.PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            return exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
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
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            // Get current extended style
            var currentExStyle = (WINDOW_EX_STYLE)Windows.Win32.PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            
            // Set or clear the flag
            var newExStyle = value ? (currentExStyle | exStyle) : (currentExStyle & ~exStyle);
            
            // Set the new extended style
            Windows.Win32.PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)newExStyle);
            
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
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
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
            return Windows.Win32.PInvoke.SetWindowPos(hwnd, hwndAfter, 0, 0, 0, 0, 
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | flags);
        }
    }
}

