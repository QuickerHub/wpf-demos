using System;
using System.Globalization;
using System.Windows;
using WindowAttach.Models;
using WindowAttach.Services;
using WindowAttach.Views;

namespace WindowAttach
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class Runner
    {
        private static WindowAttachManagerService _managerService => AppState.ManagerService;

        /// <summary>
        /// Attach window2 to window1, or detach if already attached (toggle behavior)
        /// This overload supports int handles and string placement for Quicker integration
        /// </summary>
        /// <param name="window1Handle">Handle of the target window (window to follow) as int</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows) as int</param>
        /// <param name="placement">Placement position as string (e.g., "RightTop", "LeftCenter", etc.)</param>
        /// <param name="autoUnregister">If true, automatically unregister when windows are closed (default: true)</param>
        /// <param name="offsetX">Horizontal offset (default: 0)</param>
        /// <param name="offsetY">Vertical offset (default: 0)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (default: false)</param>
        /// <param name="autoAdjustToScreen">Whether to automatically adjust position to maximize visible area when window is not fully visible (default: false)</param>
        /// <returns>True if attached, false if detached</returns>
        public static bool AttachWindow(int window1Handle, int window2Handle, string placement = "RightTop",
            bool autoUnregister = true, double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false)
        {
            // Convert int handles to IntPtr
            IntPtr hwnd1 = new IntPtr(window1Handle);
            IntPtr hwnd2 = new IntPtr(window2Handle);

            // Check if window1 and window2 are the same
            if (hwnd1 == hwnd2)
            {
                throw new ArgumentException("Window1 and Window2 cannot be the same window");
            }

            // Parse placement string to enum
            WindowPlacement placementEnum = ParsePlacement(placement);

            // Call the main AttachWindow method
            return AttachWindow(hwnd1, hwnd2, autoUnregister, placementEnum, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen);
        }

        /// <summary>
        /// Attach window2 to window1, or detach if already attached (toggle behavior)
        /// </summary>
        /// <param name="window1Handle">Handle of the target window (window to follow)</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows)</param>
        /// <param name="autoUnregister">If true, automatically unregister when windows are closed</param>
        /// <param name="placement">Placement position (default: RightTop)</param>
        /// <param name="offsetX">Horizontal offset (default: 0)</param>
        /// <param name="offsetY">Vertical offset (default: 0)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (default: false)</param>
        /// <param name="autoAdjustToScreen">Whether to automatically adjust position to maximize visible area when window is not fully visible (default: false)</param>
        /// <returns>True if attached, false if detached</returns>
        public static bool AttachWindow(IntPtr window1Handle, IntPtr window2Handle, bool autoUnregister = true,
            WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false)
        {
            if (window1Handle == IntPtr.Zero || window2Handle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handles cannot be zero");
            }

            if (window1Handle == window2Handle)
            {
                throw new ArgumentException("Window1 and Window2 cannot be the same window");
            }

            // Ensure we're on the UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                bool result = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = AttachWindow(window1Handle, window2Handle, autoUnregister, placement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen);
                });
                return result;
            }

            // Toggle attachment
            bool isAttached = _managerService.Toggle(window1Handle, window2Handle, placement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen);

            // If auto-unregister is enabled and we just attached, set up monitoring for window closure
            if (autoUnregister && isAttached)
            {
                // Note: Window closure monitoring would need to be implemented separately
                // For now, autoUnregister is a placeholder for future implementation
            }

            return isAttached;
        }

        /// <summary>
        /// Register a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window (window to follow)</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows)</param>
        /// <param name="placement">Placement position (default: RightTop)</param>
        /// <param name="offsetX">Horizontal offset (default: 0)</param>
        /// <param name="offsetY">Vertical offset (default: 0)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (default: false)</param>
        /// <param name="autoAdjustToScreen">Whether to automatically adjust position to maximize visible area when window is not fully visible (default: false)</param>
        /// <returns>True if registered successfully, false if already registered</returns>
        public static bool Register(IntPtr window1Handle, IntPtr window2Handle,
            WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false)
        {
            if (window1Handle == IntPtr.Zero || window2Handle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handles cannot be zero");
            }

            if (window1Handle == window2Handle)
            {
                throw new ArgumentException("Window1 and Window2 cannot be the same window");
            }

            // Ensure we're on the UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                bool result = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = Register(window1Handle, window2Handle, placement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen);
                });
                return result;
            }

            return _managerService.Register(window1Handle, window2Handle, placement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen);
        }

        /// <summary>
        /// Unregister a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <returns>True if unregistered successfully, false if not found</returns>
        public static bool Unregister(IntPtr window1Handle, IntPtr window2Handle)
        {
            if (window1Handle == IntPtr.Zero || window2Handle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handles cannot be zero");
            }

            // Ensure we're on the UI thread
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                bool result = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result = Unregister(window1Handle, window2Handle);
                });
                return result;
            }

            return _managerService.Unregister(window1Handle, window2Handle);
        }

        /// <summary>
        /// Check if a window attachment is registered
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <returns>True if registered</returns>
        public static bool IsRegistered(IntPtr window1Handle, IntPtr window2Handle)
        {
            if (window1Handle == IntPtr.Zero || window2Handle == IntPtr.Zero)
            {
                return false;
            }

            return _managerService.IsRegistered(window1Handle, window2Handle);
        }

        /// <summary>
        /// Unregister all attachments
        /// </summary>
        public static void UnregisterAll()
        {
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UnregisterAll();
                });
                return;
            }

            _managerService.UnregisterAll();
        }

        /// <summary>
        /// Get all registered window pairs (for internal use)
        /// </summary>
        /// <returns>List of window pairs</returns>
        internal static System.Collections.Generic.IEnumerable<(IntPtr window1Handle, IntPtr window2Handle)> GetRegisteredPairs()
        {
            return _managerService.GetRegisteredPairs();
        }

        /// <summary>
        /// Get the manager service (for internal use)
        /// </summary>
        internal static WindowAttachManagerService GetManagerService()
        {
            return AppState.ManagerService;
        }

        /// <summary>
        /// Show the window attachment list window
        /// </summary>
        public static void ShowWindowList()
        {
            if (Application.Current?.Dispatcher.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowWindowList();
                });
                return;
            }

            var window = new WindowAttachListWindow();
            window.Show();
        }

        /// <summary>
        /// Parse placement string to WindowPlacement enum
        /// </summary>
        /// <param name="placement">Placement string (case-insensitive)</param>
        /// <returns>WindowPlacement enum value, defaults to RightTop if parsing fails</returns>
        private static WindowPlacement ParsePlacement(string placement)
        {
            if (string.IsNullOrWhiteSpace(placement))
            {
                return WindowPlacement.RightTop;
            }

            // Try to parse the enum (case-insensitive)
            if (Enum.TryParse<WindowPlacement>(placement, true, out WindowPlacement result))
            {
                return result;
            }

            // If parsing fails, return default
            return WindowPlacement.RightTop;
        }
    }
}

