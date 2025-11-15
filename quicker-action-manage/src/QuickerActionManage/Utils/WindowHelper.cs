using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Window helper utilities
    /// </summary>
    public static class WindowHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_DRAWFRAME = 0x0020;

        private static Rect GetWindowRectInternal(IntPtr handle)
        {
            if (GetWindowRect(handle, out RECT rect))
            {
                return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            }
            return new Rect();
        }

        /// <summary>
        /// Set window size
        /// </summary>
        public static void SetWindowSize(IntPtr handle, int w, int h)
        {
            if (w > 0 && h > 0)
            {
                SetWindowPos(handle, IntPtr.Zero, 0, 0, w, h, SWP_NOMOVE | SWP_DRAWFRAME | SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// Get monitor work area for a window
        /// </summary>
        private static Rect? GetMonitorWorkArea(IntPtr handle)
        {
            var monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return null;

            var monitorInfo = new MONITORINFO
            {
                cbSize = Marshal.SizeOf<MONITORINFO>()
            };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return null;

            var workArea = monitorInfo.rcWork;
            return new Rect(workArea.left, workArea.top, workArea.right - workArea.left, workArea.bottom - workArea.top);
        }

        /// <summary>
        /// Center window in screen and ensure it's within screen bounds
        /// Gets the work area of the screen where the window is located
        /// </summary>
        public static void CenterWindowInScreen(IntPtr handle)
        {
            var workArea = GetMonitorWorkArea(handle);
            if (workArea == null)
            {
                // Fallback to SystemParameters if we can't get monitor info
                workArea = SystemParameters.WorkArea;
            }

            var rect = GetWindowRectInternal(handle);
            
            // Calculate center position
            double centerX = workArea.Value.Left + (workArea.Value.Width - rect.Width) / 2;
            double centerY = workArea.Value.Top + (workArea.Value.Height - rect.Height) / 2;
            
            // Ensure window is within screen bounds
            double minX = workArea.Value.Left;
            double minY = workArea.Value.Top;
            double maxX = workArea.Value.Right - rect.Width;
            double maxY = workArea.Value.Bottom - rect.Height;
            
            int x = (int)Math.Max(minX, Math.Min(maxX, centerX));
            int y = (int)Math.Max(minY, Math.Min(maxY, centerY));
            
            SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, SWP_DRAWFRAME | SWP_NOACTIVATE | SWP_NOSIZE);
        }

        /// <summary>
        /// Get window handle from WPF window
        /// </summary>
        public static IntPtr GetHandle(Window win)
        {
            return new WindowInteropHelper(win).Handle;
        }
    }
}

