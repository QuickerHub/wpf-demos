using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace WindowsTools.Services
{
    /// <summary>
    /// Service for managing desktop icons visibility
    /// </summary>
    public static class DesktopIconsService
    {
        #region Windows API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        private enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_COMMAND = 0x111;
        private static readonly IntPtr ToggleDesktopCommand = new IntPtr(0x7402);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            private int _Left;
            private int _Top;
            private int _Right;
            private int _Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler)
                : this()
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }
        }

        #endregion

        /// <summary>
        /// Toggle desktop icons visibility
        /// </summary>
        public static void Toggle()
        {
            try
            {
                IntPtr hWnd = GetWindow(FindWindow("Progman", "Program Manager"), GetWindow_Cmd.GW_CHILD);
                if (hWnd != IntPtr.Zero)
                {
                    SendMessage(hWnd, WM_COMMAND, ToggleDesktopCommand, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to toggle desktop icons: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check if desktop icons are visible
        /// </summary>
        /// <returns>True if icons are visible, false otherwise</returns>
        public static bool IsVisible()
        {
            try
            {
                IntPtr progman = FindWindow("Progman", "Program Manager");
                if (progman == IntPtr.Zero)
                    return false;

                IntPtr shellDll = GetWindow(progman, GetWindow_Cmd.GW_CHILD);
                if (shellDll == IntPtr.Zero)
                    return false;

                IntPtr folderView = GetWindow(shellDll, GetWindow_Cmd.GW_CHILD);
                if (folderView == IntPtr.Zero)
                    return false;

                WINDOWINFO info = new WINDOWINFO(null);
                info.cbSize = (uint)Marshal.SizeOf(info);
                if (GetWindowInfo(folderView, ref info))
                {
                    // Check WS_VISIBLE style (0x10000000)
                    return (info.dwStyle & 0x10000000) == 0x10000000;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Show desktop icons
        /// </summary>
        public static void Show()
        {
            if (!IsVisible())
            {
                Toggle();
            }
        }

        /// <summary>
        /// Hide desktop icons
        /// </summary>
        public static void Hide()
        {
            if (IsVisible())
            {
                Toggle();
            }
        }
    }
}

