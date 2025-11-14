using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// Window property helper
    /// </summary>
    public class WinProperty
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

        public IntPtr Handle { get; }

        public static WinProperty Default = new();
        public bool IsDefault => this == Default;

        private static readonly ConcurrentDictionary<IntPtr, WinProperty> _wps = new();

        private WinProperty()
        {
            Handle = IntPtr.Zero;
        }

        private WinProperty(IntPtr handle)
        {
            Handle = handle;
        }

        public static WinProperty Get(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return Default;
            return _wps.GetOrAdd(handle, new WinProperty(handle));
        }

        public Rect Rect
        {
            get
            {
                if (Handle == IntPtr.Zero) return new Rect();
                if (GetWindowRect(Handle, out RECT r))
                {
                    return new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
                }
                return new Rect();
            }
        }
    }
}

