using System;
using System.Windows;
using System.Windows.Interop;

namespace Quicker.Utilities
{
    /// <summary>
    /// Window helper utilities
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// Get window handle from WPF window
        /// </summary>
        public static IntPtr GetHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }

        /// <summary>
        /// Get window by handle (generic version)
        /// </summary>
        public static WType? GetWindow<WType>(IntPtr handle) where WType : class
        {
            HwndSource hwndSource = HwndSource.FromHwnd(handle);
            WType? winGet = hwndSource?.RootVisual as WType;
            return winGet;
        }

        /// <summary>
        /// Get window by handle
        /// </summary>
        public static Window? GetWindow(IntPtr handle)
        {
            return GetWindow<Window>(handle);
        }

        /// <summary>
        /// Get window by handle (int version)
        /// </summary>
        public static WType? GetWindowByHandle<WType>(int intHandle) where WType : class
        {
            return GetWindow<WType>(new IntPtr(intHandle));
        }

        /// <summary>
        /// Get window by handle (int version, non-generic)
        /// </summary>
        public static Window? GetWindowByHandle(int intHandle)
        {
            return GetWindow(new IntPtr(intHandle));
        }
    }
}

