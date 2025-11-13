using WindowEdgeHide.Models;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Extension methods for ManagedWindow
    /// </summary>
    public static class ManagedWindowExtensions
    {
        /// <summary>
        /// Check if window is minimized
        /// </summary>
        public static bool IsMinimized(this ManagedWindow managedWindow)
        {
            return managedWindow.WindowState == Models.WindowState.Minimized;
        }

        /// <summary>
        /// Check if window is maximized
        /// </summary>
        public static bool IsMaximized(this ManagedWindow managedWindow)
        {
            return managedWindow.WindowState == Models.WindowState.Maximized;
        }

        /// <summary>
        /// Check if window is in normal state
        /// </summary>
        public static bool IsNormal(this ManagedWindow managedWindow)
        {
            return managedWindow.WindowState == Models.WindowState.Normal;
        }

        /// <summary>
        /// Check if window is visible and not minimized
        /// </summary>
        public static bool IsVisibleAndNotMinimized(this ManagedWindow managedWindow)
        {
            return managedWindow.IsVisible && managedWindow.WindowState != Models.WindowState.Minimized;
        }
    }
}

