using System;
using System.Collections.Generic;
using System.Linq;
using WindowAttach.Models;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for adjusting window position to maximize visible area on screen
    /// </summary>
    internal static class ScreenAdjustHelper
    {
        /// <summary>
        /// Calculate the intersection area between two rectangles
        /// </summary>
        /// <param name="rect1">First rectangle</param>
        /// <param name="rect2">Second rectangle</param>
        /// <returns>Intersection area (0 if no intersection)</returns>
        private static int CalculateIntersectionArea(WindowRect rect1, WindowRect rect2)
        {
            int left = Math.Max(rect1.Left, rect2.Left);
            int top = Math.Max(rect1.Top, rect2.Top);
            int right = Math.Min(rect1.Right, rect2.Right);
            int bottom = Math.Min(rect1.Bottom, rect2.Bottom);

            if (left >= right || top >= bottom)
                return 0;

            return (right - left) * (bottom - top);
        }

        /// <summary>
        /// Calculate visible area of window2 on screen, excluding overlap with window1
        /// </summary>
        /// <param name="window2Rect">Window2 rectangle</param>
        /// <param name="screenRect">Screen rectangle</param>
        /// <param name="window1Rect">Window1 rectangle (to exclude overlap)</param>
        /// <returns>Visible area excluding overlap with window1</returns>
        private static int CalculateVisibleArea(WindowRect window2Rect, WindowRect screenRect, WindowRect window1Rect)
        {
            // Calculate intersection area between window2 and screen
            int screenIntersectionArea = CalculateIntersectionArea(window2Rect, screenRect);
            
            // Calculate intersection area between window2 and window1
            int window1IntersectionArea = CalculateIntersectionArea(window2Rect, window1Rect);
            
            // Visible area = screen intersection - window1 intersection
            // But we need to make sure we only subtract the part that's also within the screen
            // So we calculate the intersection of (window2 ∩ screen) and (window2 ∩ window1)
            // This is the area that's both on screen and overlapping with window1
            
            // Calculate the intersection rectangle of window2 and screen
            int screenLeft = Math.Max(window2Rect.Left, screenRect.Left);
            int screenTop = Math.Max(window2Rect.Top, screenRect.Top);
            int screenRight = Math.Min(window2Rect.Right, screenRect.Right);
            int screenBottom = Math.Min(window2Rect.Bottom, screenRect.Bottom);
            
            if (screenLeft >= screenRight || screenTop >= screenBottom)
                return 0;
            
            var screenIntersectionRect = new WindowRect(screenLeft, screenTop, screenRight, screenBottom);
            
            // Calculate overlap between screen intersection and window1
            int overlapArea = CalculateIntersectionArea(screenIntersectionRect, window1Rect);
            
            // Visible area = screen intersection - overlap with window1
            return screenIntersectionArea - overlapArea;
        }

        /// <summary>
        /// Check if a window is completely within a screen
        /// </summary>
        /// <param name="windowRect">Window rectangle</param>
        /// <param name="screenRect">Screen rectangle</param>
        /// <returns>True if window is completely within screen</returns>
        private static bool IsWindowFullyVisible(WindowRect windowRect, WindowRect screenRect)
        {
            return windowRect.Left >= screenRect.Left &&
                   windowRect.Top >= screenRect.Top &&
                   windowRect.Right <= screenRect.Right &&
                   windowRect.Bottom <= screenRect.Bottom;
        }

        /// <summary>
        /// Get the opposite placement (e.g., LeftTop -> RightTop)
        /// </summary>
        private static WindowPlacement GetOppositePlacement(WindowPlacement placement)
        {
            return placement switch
            {
                WindowPlacement.LeftTop => WindowPlacement.RightTop,
                WindowPlacement.LeftCenter => WindowPlacement.RightCenter,
                WindowPlacement.LeftBottom => WindowPlacement.RightBottom,
                WindowPlacement.TopLeft => WindowPlacement.BottomLeft,
                WindowPlacement.TopCenter => WindowPlacement.BottomCenter,
                WindowPlacement.TopRight => WindowPlacement.BottomRight,
                WindowPlacement.RightTop => WindowPlacement.LeftTop,
                WindowPlacement.RightCenter => WindowPlacement.LeftCenter,
                WindowPlacement.RightBottom => WindowPlacement.LeftBottom,
                WindowPlacement.BottomLeft => WindowPlacement.TopLeft,
                WindowPlacement.BottomCenter => WindowPlacement.TopCenter,
                WindowPlacement.BottomRight => WindowPlacement.TopRight,
                _ => placement
            };
        }

        /// <summary>
        /// Get placements in priority order based on original placement direction
        /// For right**: prioritize left**, then top** and bottom**
        /// For left**: prioritize right**, then top** and bottom**
        /// For top**: prioritize bottom**, then left** and right**
        /// For bottom**: prioritize top**, then left** and right**
        /// </summary>
        private static WindowPlacement[] GetPlacementsInPriorityOrder(WindowPlacement originalPlacement)
        {
            // Group placements by direction
            var leftPlacements = new[] { WindowPlacement.LeftTop, WindowPlacement.LeftCenter, WindowPlacement.LeftBottom };
            var rightPlacements = new[] { WindowPlacement.RightTop, WindowPlacement.RightCenter, WindowPlacement.RightBottom };
            var topPlacements = new[] { WindowPlacement.TopLeft, WindowPlacement.TopCenter, WindowPlacement.TopRight };
            var bottomPlacements = new[] { WindowPlacement.BottomLeft, WindowPlacement.BottomCenter, WindowPlacement.BottomRight };

            var priorityList = new List<WindowPlacement>();

            // Determine priority based on original placement direction
            if (rightPlacements.Contains(originalPlacement))
            {
                // For right**: prioritize left**, then top** and bottom**
                priorityList.AddRange(leftPlacements);
                priorityList.AddRange(topPlacements);
                priorityList.AddRange(bottomPlacements);
                priorityList.AddRange(rightPlacements.Where(p => p != originalPlacement));
            }
            else if (leftPlacements.Contains(originalPlacement))
            {
                // For left**: prioritize right**, then top** and bottom**
                priorityList.AddRange(rightPlacements);
                priorityList.AddRange(topPlacements);
                priorityList.AddRange(bottomPlacements);
                priorityList.AddRange(leftPlacements.Where(p => p != originalPlacement));
            }
            else if (topPlacements.Contains(originalPlacement))
            {
                // For top**: prioritize bottom**, then left** and right**
                priorityList.AddRange(bottomPlacements);
                priorityList.AddRange(leftPlacements);
                priorityList.AddRange(rightPlacements);
                priorityList.AddRange(topPlacements.Where(p => p != originalPlacement));
            }
            else if (bottomPlacements.Contains(originalPlacement))
            {
                // For bottom**: prioritize top**, then left** and right**
                priorityList.AddRange(topPlacements);
                priorityList.AddRange(leftPlacements);
                priorityList.AddRange(rightPlacements);
                priorityList.AddRange(bottomPlacements.Where(p => p != originalPlacement));
            }
            else
            {
                // Fallback: use opposite placement first, then all others
                var oppositePlacement = GetOppositePlacement(originalPlacement);
                priorityList.Add(oppositePlacement);
                var allPlacements = new[]
                {
                    WindowPlacement.LeftTop, WindowPlacement.LeftCenter, WindowPlacement.LeftBottom,
                    WindowPlacement.TopLeft, WindowPlacement.TopCenter, WindowPlacement.TopRight,
                    WindowPlacement.RightTop, WindowPlacement.RightCenter, WindowPlacement.RightBottom,
                    WindowPlacement.BottomLeft, WindowPlacement.BottomCenter, WindowPlacement.BottomRight
                };
                foreach (var placement in allPlacements)
                {
                    if (placement != oppositePlacement && placement != originalPlacement)
                    {
                        priorityList.Add(placement);
                    }
                }
            }

            return priorityList.ToArray();
        }

        /// <summary>
        /// Find the best position for window2 to maximize visible area on screen, excluding overlap with window1
        /// Tries all placement positions and selects the one with maximum visible area
        /// Prioritizes opposite placement (e.g., LeftTop -> RightTop)
        /// </summary>
        /// <param name="idealX">Ideal X position based on placement</param>
        /// <param name="idealY">Ideal Y position based on placement</param>
        /// <param name="window2Width">Window2 width</param>
        /// <param name="window2Height">Window2 height</param>
        /// <param name="screenRect">Screen rectangle (work area)</param>
        /// <param name="window1Rect">Window1 rectangle (to exclude overlap)</param>
        /// <param name="originalPlacement">Original placement (to prioritize opposite placement)</param>
        /// <param name="offsetX">Horizontal offset</param>
        /// <param name="offsetY">Vertical offset</param>
        /// <returns>Best position (x, y) that maximizes visible area</returns>
        internal static (int x, int y) FindBestPosition(int idealX, int idealY, int window2Width, int window2Height, WindowRect screenRect, WindowRect window1Rect, WindowPlacement originalPlacement, double offsetX = 0, double offsetY = 0)
        {
            // Create ideal window rectangle
            var idealWindowRect = new WindowRect(idealX, idealY, idealX + window2Width, idealY + window2Height);

            // Only adjust position if window2 is partially outside the screen
            // If window2 is fully within screen bounds, use ideal position regardless of overlap with window1
            if (IsWindowFullyVisible(idealWindowRect, screenRect))
            {
                // Window is fully visible on screen, use ideal position
                return (idealX, idealY);
            }

            // Window is partially outside screen, try to find better position
            // Calculate visible area at ideal position (excluding overlap with window1)
            int idealVisibleArea = CalculateVisibleArea(idealWindowRect, screenRect, window1Rect);

            // Try all placement positions to find the best one, prioritizing opposite placement
            int bestX = idealX;
            int bestY = idealY;
            int maxVisibleArea = idealVisibleArea;

            // Get placements in priority order (opposite first)
            var placementsInPriority = GetPlacementsInPriorityOrder(originalPlacement);

            // Try each placement position in priority order
            foreach (var placement in placementsInPriority)
            {
                // Calculate position for this placement
                var (x, y) = PlacementCalculator.CalculateTargetPosition(window1Rect, window2Width, window2Height, placement, offsetX, offsetY);
                
                // Create window rectangle for this position
                var candidateRect = new WindowRect(x, y, x + window2Width, y + window2Height);
                
                // Calculate visible area (excluding overlap with window1)
                int visibleArea = CalculateVisibleArea(candidateRect, screenRect, window1Rect);
                
                if (visibleArea > maxVisibleArea)
                {
                    maxVisibleArea = visibleArea;
                    bestX = x;
                    bestY = y;
                }
            }

            return (bestX, bestY);
        }

        /// <summary>
        /// Adjust window position to maximize visible area on screen, excluding overlap with window1
        /// Tries all placement positions and selects the one with maximum visible area
        /// Prioritizes opposite placement (e.g., LeftTop -> RightTop)
        /// </summary>
        /// <param name="idealX">Ideal X position based on placement</param>
        /// <param name="idealY">Ideal Y position based on placement</param>
        /// <param name="window2Width">Window2 width</param>
        /// <param name="window2Height">Window2 height</param>
        /// <param name="window2Handle">Window2 handle (to get screen info)</param>
        /// <param name="window1Rect">Window1 rectangle (to exclude overlap)</param>
        /// <param name="originalPlacement">Original placement (to prioritize opposite placement)</param>
        /// <param name="offsetX">Horizontal offset</param>
        /// <param name="offsetY">Vertical offset</param>
        /// <returns>Adjusted position (x, y) that maximizes visible area, or ideal position if fully visible</returns>
        internal static (int x, int y) AdjustPositionToScreen(int idealX, int idealY, int window2Width, int window2Height, IntPtr window2Handle, WindowRect window1Rect, WindowPlacement originalPlacement, double offsetX = 0, double offsetY = 0)
        {
            // Get screen work area for window2
            var screenRect = WindowHelper.GetMonitorWorkArea(window2Handle);
            if (screenRect == null)
            {
                // If we can't get screen info, return ideal position
                return (idealX, idealY);
            }

            return FindBestPosition(idealX, idealY, window2Width, window2Height, screenRect.Value, window1Rect, originalPlacement, offsetX, offsetY);
        }
    }
}

