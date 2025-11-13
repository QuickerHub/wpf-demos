using System;
using WindowEdgeHide.Models;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide.Utils
{
    /// <summary>
    /// Helper class for calculating edge positions
    /// </summary>
    internal static class EdgeCalculator
    {
        /// <summary>
        /// Threshold distance to consider window at screen edge (in pixels)
        /// </summary>
        private const int EdgeThreshold = 10;

        /// <summary>
        /// Check if window is at screen edge (triggering edge hide)
        /// Window is considered at edge if it's partially or fully outside screen bounds,
        /// or within threshold distance from screen edge
        /// Excludes taskbar edge from consideration
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <returns>True if window is at screen edge (excluding taskbar edge)</returns>
        internal static bool IsWindowAtEdge(IntPtr windowHandle)
        {
            var windowRect = WindowHelper.GetWindowRect(windowHandle);
            if (windowRect == null)
                return false;

            var screenRect = WindowHelper.GetMonitorWorkArea(windowHandle);
            if (screenRect == null)
                return false;

            // Get taskbar edge to exclude it
            var taskbarEdge = WindowHelper.GetTaskbarEdge(windowHandle);

            var rect = windowRect.Value;
            var screen = screenRect.Value;

            // Check if window is at screen edge using extension method
            rect.CheckEdges(screen, EdgeThreshold, out bool atLeft, out bool atTop, out bool atRight, out bool atBottom);

            // Exclude taskbar edge and screen boundaries from consideration
            bool atTopEdge = atTop && taskbarEdge != EdgeDirection.Top && 
                            !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Top);
            
            bool atLeftEdge = atLeft && taskbarEdge != EdgeDirection.Left && 
                             !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Left);
            
            bool atRightEdge = atRight && taskbarEdge != EdgeDirection.Right && 
                              !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Right);
            
            bool atBottomEdge = atBottom && taskbarEdge != EdgeDirection.Bottom && 
                               !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Bottom);

            return atTopEdge || atLeftEdge || atRightEdge || atBottomEdge;
        }

        /// <summary>
        /// Find the edge direction based on current window position
        /// Determines which edge the window is crossing
        /// Excludes taskbar edge from consideration
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <returns>Edge direction</returns>
        internal static EdgeDirection FindNearestEdge(IntPtr windowHandle)
        {
            var windowRect = WindowHelper.GetWindowRect(windowHandle);
            if (windowRect == null)
                return EdgeDirection.Right; // Default to right

            var screenRect = WindowHelper.GetMonitorWorkArea(windowHandle);
            if (screenRect == null)
                return EdgeDirection.Right; // Default to right

            // Get taskbar edge to exclude it
            var taskbarEdge = WindowHelper.GetTaskbarEdge(windowHandle);

            var rect = windowRect.Value;
            var screen = screenRect.Value;

            // Check which edge the window is crossing
            // Priority: Top > Left > Right > Bottom (check in order)
            // Exclude taskbar edge and screen boundaries from consideration
            
            // Top edge: window top < screen top (if not taskbar edge and not between screens)
            if (taskbarEdge != EdgeDirection.Top && 
                !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Top) &&
                rect.Top < screen.Top)
                return EdgeDirection.Top;
            
            // Left edge: window left < screen left (if not taskbar edge and not between screens)
            if (taskbarEdge != EdgeDirection.Left && 
                !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Left) &&
                rect.Left < screen.Left)
                return EdgeDirection.Left;
            
            // Right edge: window right > screen right (if not taskbar edge and not between screens)
            if (taskbarEdge != EdgeDirection.Right && 
                !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Right) &&
                rect.Right > screen.Right)
                return EdgeDirection.Right;
            
            // Bottom edge: window bottom > screen bottom (if not taskbar edge and not between screens)
            if (taskbarEdge != EdgeDirection.Bottom && 
                !WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Bottom) &&
                rect.Bottom > screen.Bottom)
                return EdgeDirection.Bottom;

            // If window is fully within screen, find nearest edge by distance
            // Exclude taskbar edge and screen boundaries from distance calculation
            // Create edge rectangles for distance calculation
            int distToLeft = (taskbarEdge == EdgeDirection.Left || WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Left)) 
                ? int.MaxValue : Math.Abs(rect.Left - screen.Left);
            int distToTop = (taskbarEdge == EdgeDirection.Top || WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Top)) 
                ? int.MaxValue : Math.Abs(rect.Top - screen.Top);
            int distToRight = (taskbarEdge == EdgeDirection.Right || WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Right)) 
                ? int.MaxValue : Math.Abs(screen.Right - rect.Right);
            int distToBottom = (taskbarEdge == EdgeDirection.Bottom || WindowHelper.IsEdgeBetweenScreens(windowHandle, EdgeDirection.Bottom)) 
                ? int.MaxValue : Math.Abs(screen.Bottom - rect.Bottom);

            int minDist = Math.Min(Math.Min(distToLeft, distToTop), Math.Min(distToRight, distToBottom));

            if (minDist == distToLeft) return EdgeDirection.Left;
            if (minDist == distToTop) return EdgeDirection.Top;
            if (minDist == distToRight) return EdgeDirection.Right;
            return EdgeDirection.Bottom;
        }

        /// <summary>
        /// Calculate edge position for window (visible position at screen edge)
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edge">Edge direction</param>
        /// <param name="visibleArea">Visible area thickness when hidden</param>
        /// <returns>Window position (x, y, width, height)</returns>
        internal static (int x, int y, int width, int height) CalculateEdgePosition(
            IntPtr windowHandle, EdgeDirection edge, IntThickness visibleArea)
        {
            var windowRect = WindowHelper.GetWindowRect(windowHandle);
            if (windowRect == null)
                return (0, 0, 0, 0);

            var screenRect = WindowHelper.GetMonitorWorkArea(windowHandle);
            if (screenRect == null)
                return (0, 0, 0, 0);

            var rect = windowRect.Value;
            var screen = screenRect.Value;
            int width = rect.Width;
            int height = rect.Height;

            return edge switch
            {
                EdgeDirection.Left => (screen.Left, rect.Top, width, height),
                EdgeDirection.Top => (rect.Left, screen.Top, width, height),
                EdgeDirection.Right => (screen.Right - width, rect.Top, width, height),
                EdgeDirection.Bottom => (rect.Left, screen.Bottom - height, width, height),
                _ => (rect.Left, rect.Top, width, height)
            };
        }

        /// <summary>
        /// Calculate hidden position for window (moved outside screen, only small part visible)
        /// Only modifies the coordinate in the edge direction, keeps the other coordinate unchanged
        /// Ensures window stays within screen bounds by adjusting the other coordinate if needed
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edge">Edge direction</param>
        /// <param name="visibleArea">Visible area thickness when hidden</param>
        /// <returns>Window position (x, y, width, height)</returns>
        internal static (int x, int y, int width, int height) CalculateHiddenPosition(
            IntPtr windowHandle, EdgeDirection edge, IntThickness visibleArea)
        {
            var windowRect = WindowHelper.GetWindowRect(windowHandle);
            if (windowRect == null)
                return (0, 0, 0, 0);

            var screenRect = WindowHelper.GetMonitorWorkArea(windowHandle);
            if (screenRect == null)
                return (0, 0, 0, 0);

            var rect = windowRect.Value;
            var screen = screenRect.Value;
            int width = rect.Width;
            int height = rect.Height;

            int x = rect.Left;
            int y = rect.Top;

            switch (edge)
            {
                case EdgeDirection.Left:
                    // Only modify x coordinate, keep y unchanged
                    x = screen.Left - width + visibleArea.Left;
                    // Ensure window stays within screen bounds by adjusting y if needed
                    if (y < screen.Top)
                        y = screen.Top;
                    if (y + height > screen.Bottom)
                        y = screen.Bottom - height;
                    break;

                case EdgeDirection.Top:
                    // Only modify y coordinate, keep x unchanged
                    y = screen.Top - height + visibleArea.Top;
                    // Ensure window stays within screen bounds by adjusting x if needed
                    if (x < screen.Left)
                        x = screen.Left;
                    if (x + width > screen.Right)
                        x = screen.Right - width;
                    break;

                case EdgeDirection.Right:
                    // Only modify x coordinate, keep y unchanged
                    x = screen.Right - visibleArea.Right;
                    // Ensure window stays within screen bounds by adjusting y if needed
                    if (y < screen.Top)
                        y = screen.Top;
                    if (y + height > screen.Bottom)
                        y = screen.Bottom - height;
                    break;

                case EdgeDirection.Bottom:
                    // Only modify y coordinate, keep x unchanged
                    y = screen.Bottom - visibleArea.Bottom;
                    // Ensure window stays within screen bounds by adjusting x if needed
                    if (x < screen.Left)
                        x = screen.Left;
                    if (x + width > screen.Right)
                        x = screen.Right - width;
                    break;
            }

            return (x, y, width, height);
        }
    }
}

