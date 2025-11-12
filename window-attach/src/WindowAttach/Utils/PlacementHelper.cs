using WindowAttach.Models;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for calculating popup placement based on main attachment placement
    /// </summary>
    internal static class PlacementHelper
    {
        /// <summary>
        /// Calculate popup placement based on main attachment placement
        /// Popup should be positioned so that window1, window2, and popup don't overlap
        /// </summary>
        /// <param name="mainPlacement">Main attachment placement (window2 relative to window1)</param>
        /// <returns>Popup placement (popup relative to window2)</returns>
        public static WindowPlacement GetPopupPlacement(WindowPlacement mainPlacement)
        {
            // Map main placement to popup placement
            // Popup should be positioned at the corner opposite to where window2 connects to window1
            // This ensures window1, window2, and popup don't overlap
            return mainPlacement switch
            {
                // Window2 is on the left of window1, popup should be on the right of window2 (opposite side)
                WindowPlacement.LeftTop => WindowPlacement.BottomCenter,      // Opposite corner
                WindowPlacement.LeftCenter => WindowPlacement.BottomCenter,   // Opposite side, same vertical position
                WindowPlacement.LeftBottom => WindowPlacement.TopCenter,      // Opposite corner

                // Window2 is on the top of window1, popup should be on the bottom of window2 (opposite side)
                WindowPlacement.TopLeft => WindowPlacement.RightCenter,      // Bottom-right corner
                WindowPlacement.TopCenter => WindowPlacement.RightCenter,    // Bottom-right corner
                WindowPlacement.TopRight => WindowPlacement.LeftCenter,      // Bottom-left corner

                // Window2 is on the right of window1, popup should be on the left of window2 (opposite side)
                WindowPlacement.RightTop => WindowPlacement.BottomCenter,      // Opposite corner
                WindowPlacement.RightCenter => WindowPlacement.BottomCenter,   // Opposite side, same vertical position
                WindowPlacement.RightBottom => WindowPlacement.TopCenter,      // Opposite corner

                // Window2 is on the bottom of window1, popup should be on the top of window2 (opposite side)
                WindowPlacement.BottomLeft => WindowPlacement.RightCenter,      // Top-right corner
                WindowPlacement.BottomCenter => WindowPlacement.RightCenter,   // Top center
                WindowPlacement.BottomRight => WindowPlacement.LeftCenter,      // Top-left corner

                _ => WindowPlacement.BottomRight // Default fallback
            };
        }
    }
}

