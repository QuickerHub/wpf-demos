using System;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowEdgeHide.Models;
using WINDOW_LONG_PTR_INDEX = Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX;
using WINDOW_EX_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;

namespace WindowEdgeHide.Utils
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

            return Windows.Win32.PInvoke.SetWindowPos(hwnd, HWND.Null, x, y, width, height, flags);
        }

        /// <summary>
        /// Get window topmost state
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>True if window is topmost, false otherwise</returns>
        internal static bool GetWindowTopmost(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            // Get window extended style
            var exStyle = GetWindowLongPtrWrapper(hWnd, (int)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            return ((WINDOW_EX_STYLE)exStyle).HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST);
        }

        /// <summary>
        /// Set window topmost state
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="topmost">True to set topmost, false to remove topmost</param>
        /// <returns>True if successful</returns>
        internal static bool SetWindowTopmost(IntPtr hWnd, bool topmost)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            // Use SetWindowPos with HWND_TOPMOST or HWND_NOTOPMOST
            var insertAfter = topmost ? new HWND(new IntPtr(-1)) : new HWND(new IntPtr(-2)); // -1 = HWND_TOPMOST, -2 = HWND_NOTOPMOST
            return Windows.Win32.PInvoke.SetWindowPos(
                hwnd,
                insertAfter,
                0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }

        /// <summary>
        /// Get monitor work area for a window
        /// First tries MonitorFromWindow (most reliable when window intersects with screen)
        /// If window is completely off-screen, uses distance calculation to find nearest monitor
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

            // Get window rectangle first
            var windowRect = GetWindowRect(hWnd);
            if (windowRect == null)
                return null;

            // First try MonitorFromWindow (most reliable when window intersects with screen)
            // This handles the case where window intersects with multiple screens (distance = 0 for both)
            var monitor = MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitor.Value != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>()
                };
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    return new WindowRect(
                        monitorInfo.rcWork.left,
                        monitorInfo.rcWork.top,
                        monitorInfo.rcWork.right,
                        monitorInfo.rcWork.bottom
                    );
                }
            }

            // If MonitorFromWindow failed or window is completely off-screen,
            // use EnumDisplayMonitors to find the monitor with minimum distance
            WindowRect? result = null;
            var targetRect = windowRect.Value;
            int minDistance = int.MaxValue;

            unsafe
            {
                RECT* nullRect = null;
                EnumDisplayMonitors(
                    default(HDC),
                    nullRect,
                    (HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM lParam) =>
                    {
                        var monitorInfo = new MONITORINFO
                        {
                            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>()
                        };
                        if (!GetMonitorInfo(hMonitor, ref monitorInfo))
                            return true; // Continue enumeration

                        var monitorRect = new WindowRect(
                            monitorInfo.rcMonitor.left,
                            monitorInfo.rcMonitor.top,
                            monitorInfo.rcMonitor.right,
                            monitorInfo.rcMonitor.bottom
                        );

                        // Calculate distance between window and monitor
                        int distance = targetRect.Distance(monitorRect);

                        // Keep track of the monitor with the minimum distance
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            result = new WindowRect(
                                monitorInfo.rcWork.left,
                                monitorInfo.rcWork.top,
                                monitorInfo.rcWork.right,
                                monitorInfo.rcWork.bottom
                            );
                        }

                        return true; // Continue enumeration to find the best match
                    },
                    default(LPARAM)
                );
            }

            return result;
        }

        /// <summary>
        /// Check if an edge is between two screens (screen boundary)
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="edge">Edge direction to check</param>
        /// <returns>True if the edge is between two screens</returns>
        internal static bool IsEdgeBetweenScreens(IntPtr hWnd, EdgeDirection edge)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            var windowRect = GetWindowRect(hWnd);
            if (windowRect == null)
                return false;

            var screenRect = GetMonitorWorkArea(hWnd);
            if (screenRect == null)
                return false;

            // Get current monitor bounds
            var monitor = MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            if (monitor.Value == IntPtr.Zero)
                return false;

            var monitorInfo = new MONITORINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>()
            };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return false;

            var currentMonitorRect = monitorInfo.rcMonitor;
            int edgeCoordinate = 0;

            // Get the coordinate of the edge we're checking
            switch (edge)
            {
                case EdgeDirection.Left:
                    edgeCoordinate = screenRect.Value.Left;
                    break;
                case EdgeDirection.Top:
                    edgeCoordinate = screenRect.Value.Top;
                    break;
                case EdgeDirection.Right:
                    edgeCoordinate = screenRect.Value.Right;
                    break;
                case EdgeDirection.Bottom:
                    edgeCoordinate = screenRect.Value.Bottom;
                    break;
                default:
                    return false;
            }

            // Enumerate all monitors and check if any other monitor's edge overlaps with this edge
            bool foundAdjacentScreen = false;
            unsafe
            {
                MONITORENUMPROC callback = delegate (HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM lParam)
                {
                    var otherMonitorInfo = new MONITORINFO
                    {
                        cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>()
                    };
                    if (!GetMonitorInfo(hMonitor, ref otherMonitorInfo))
                        return true; // Continue enumeration

                    var otherRect = otherMonitorInfo.rcMonitor;

                    // Check if this edge overlaps with another monitor's opposite edge
                    switch (edge)
                    {
                        case EdgeDirection.Left:
                            // Check if screen's left edge matches another screen's right edge
                            if (edgeCoordinate == otherRect.right && currentMonitorRect.left != otherRect.left)
                                foundAdjacentScreen = true;
                            break;
                        case EdgeDirection.Top:
                            // Check if screen's top edge matches another screen's bottom edge
                            if (edgeCoordinate == otherRect.bottom && currentMonitorRect.top != otherRect.top)
                                foundAdjacentScreen = true;
                            break;
                        case EdgeDirection.Right:
                            // Check if screen's right edge matches another screen's left edge
                            if (edgeCoordinate == otherRect.left && currentMonitorRect.right != otherRect.right)
                                foundAdjacentScreen = true;
                            break;
                        case EdgeDirection.Bottom:
                            // Check if screen's bottom edge matches another screen's top edge
                            if (edgeCoordinate == otherRect.top && currentMonitorRect.bottom != otherRect.bottom)
                                foundAdjacentScreen = true;
                            break;
                    }

                    return !foundAdjacentScreen; // Stop enumeration if found
                };

                RECT* nullRect = null;
                EnumDisplayMonitors(default(HDC), nullRect, callback, default(LPARAM));
            }

            return foundAdjacentScreen;
        }

        /// <summary>
        /// Get taskbar edge direction for a window's monitor
        /// Returns the edge where taskbar is located, or null if taskbar is not docked or auto-hide
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>Taskbar edge direction, or null if not detected</returns>
        internal static EdgeDirection? GetTaskbarEdge(IntPtr hWnd)
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

            var rcMonitor = monitorInfo.rcMonitor;
            var rcWork = monitorInfo.rcWork;

            // Compare monitor bounds with work area to determine taskbar position
            // Taskbar reduces the work area, so we check which edge is different

            // Top edge: work area top > monitor top
            if (rcWork.top > rcMonitor.top)
                return EdgeDirection.Top;

            // Left edge: work area left > monitor left
            if (rcWork.left > rcMonitor.left)
                return EdgeDirection.Left;

            // Right edge: work area right < monitor right
            if (rcWork.right < rcMonitor.right)
                return EdgeDirection.Right;

            // Bottom edge: work area bottom < monitor bottom
            if (rcWork.bottom < rcMonitor.bottom)
                return EdgeDirection.Bottom;

            // No taskbar detected (work area equals monitor area)
            return null;
        }

        /// <summary>
        /// Get cursor position in screen coordinates
        /// </summary>
        /// <returns>Cursor position, or null if failed</returns>
        internal static (int x, int y)? GetCursorPos()
        {
            try
            {
                var pos = System.Windows.Forms.Cursor.Position;
                return (pos.X, pos.Y);
            }
            catch
            {
                // Fallback: try using WPF Mouse (less accurate)
                try
                {
                    var wpfPos = System.Windows.Input.Mouse.GetPosition(null);
                    return ((int)wpfPos.X, (int)wpfPos.Y);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Check if point is within window rectangle
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <param name="rect">Window rectangle</param>
        /// <returns>True if point is within rectangle</returns>
        internal static bool IsPointInRect((int x, int y) point, WindowRect rect)
        {
            return rect.Contains(point);
        }

        /// <summary>
        /// Get foreground window handle
        /// </summary>
        /// <returns>Foreground window handle, or IntPtr.Zero if failed</returns>
        internal static IntPtr GetForegroundWindow()
        {
            var hwnd = Windows.Win32.PInvoke.GetForegroundWindow();
            return hwnd.Value;
        }

        /// <summary>
        /// Check if window is minimized
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns>True if window is minimized, false otherwise</returns>
        internal static bool IsWindowMinimized(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            return Windows.Win32.PInvoke.IsIconic(hwnd);
        }

        /// <summary>
        /// Get window long pointer value
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="nIndex">Index to retrieve (GWLP_WNDPROC, etc.)</param>
        /// <returns>Window long pointer value</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        internal static IntPtr GetWindowLongPtrWrapper(IntPtr hWnd, int nIndex)
        {
            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return IntPtr.Zero;

            return GetWindowLongPtr(hWnd, nIndex);
        }

        /// <summary>
        /// Set window long pointer value
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="nIndex">Index to set (GWLP_WNDPROC, etc.)</param>
        /// <param name="dwNewLong">New value</param>
        /// <returns>Previous value</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        internal static IntPtr SetWindowLongPtrWrapper(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return IntPtr.Zero;

            return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
        }

        /// <summary>
        /// Call window procedure
        /// </summary>
        internal static LRESULT CallWindowProc(WNDPROC lpPrevWndFunc, HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            return Windows.Win32.PInvoke.CallWindowProc(lpPrevWndFunc, hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Default window procedure
        /// </summary>
        internal static LRESULT DefWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            return Windows.Win32.PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Get DefWindowProc address (for fallback when original proc is DefWindowProc)
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        internal static IntPtr GetDefWindowProcAddress()
        {
            // Return a marker value that indicates DefWindowProc should be used
            // We'll check for this in the calling code
            return new IntPtr(-1);
        }

        /// <summary>
        /// TRACKMOUSEEVENT structure
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct TRACKMOUSEEVENT_STRUCT
        {
            public uint cbSize;
            public uint dwFlags;
            public HWND hwndTrack;
            public uint dwHoverTime;
        }

        /// <summary>
        /// Track mouse event - using DllImport directly since TRACKMOUSEEVENT may not be generated
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool TrackMouseEventInternal(ref TRACKMOUSEEVENT_STRUCT lpEventTrack);

        /// <summary>
        /// Track mouse event wrapper
        /// </summary>
        internal static bool TrackMouseEvent(ref TRACKMOUSEEVENT_STRUCT lpEventTrack)
        {
            return TrackMouseEventInternal(ref lpEventTrack);
        }

        /// <summary>
        /// Set window extended style
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="exStyle">Extended style to set</param>
        /// <param name="value">True to add the style, false to remove it</param>
        /// <returns>True if successful</returns>
        internal static bool SetWindowExStyle(IntPtr hWnd, WINDOW_EX_STYLE exStyle, bool value)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var hwnd = new HWND(hWnd);
            if (!IsWindow(hwnd))
                return false;

            // Get current extended style using wrapper method
            var currentStyle = GetWindowLongPtrWrapper(hWnd, (int)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            var currentExStyle = (WINDOW_EX_STYLE)currentStyle;

            // Set or clear the flag
            var newExStyle = value ? (currentExStyle | exStyle) : (currentExStyle & ~exStyle);

            // Set the new extended style using wrapper method
            SetWindowLongPtrWrapper(hWnd, (int)WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (IntPtr)newExStyle);

            return true;
        }
    }
}

