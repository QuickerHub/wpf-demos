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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
        /// Move window to specified position and ensure it's in screen
        /// </summary>
        public static void MoveWindowInToScreen(IntPtr handle, int x, int y)
        {
            var point = new System.Drawing.Point(x, y);
            var workArea = SystemParameters.WorkArea;
            var rect = GetWindowRectInternal(handle);
            
            // Limit point to work area, accounting for window size
            double minX = workArea.Left;
            double minY = workArea.Top;
            double maxX = workArea.Right - rect.Width;
            double maxY = workArea.Bottom - rect.Height;
            
            if (point.X < minX) point.X = (int)minX;
            if (point.Y < minY) point.Y = (int)minY;
            if (point.X > maxX) point.X = (int)maxX;
            if (point.Y > maxY) point.Y = (int)maxY;
            
            MoveWindow(handle, point);
        }

        private static void MoveWindow(IntPtr handle, System.Drawing.Point p)
        {
            MoveWindow(handle, p.X, p.Y);
        }

        private static void MoveWindow(IntPtr handle, int x, int y)
        {
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

