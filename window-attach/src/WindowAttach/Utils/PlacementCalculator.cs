using System;
using System.Linq;
using WindowAttach.Models;

namespace WindowAttach.Utils
{
    /// <summary>
    /// Helper class for calculating optimal window placement
    /// </summary>
    internal static class PlacementCalculator
    {
        /// <summary>
        /// Calculate the target position for window2 based on placement
        /// </summary>
        /// <param name="window1Rect">Window1 rectangle</param>
        /// <param name="window2Width">Window2 width</param>
        /// <param name="window2Height">Window2 height</param>
        /// <param name="placement">Placement option</param>
        /// <param name="offsetX">Horizontal offset</param>
        /// <param name="offsetY">Vertical offset</param>
        /// <returns>Target position (x, y) for window2</returns>
        internal static (int x, int y) CalculateTargetPosition(WindowRect window1Rect, int window2Width, int window2Height, 
            WindowPlacement placement, double offsetX = 0, double offsetY = 0)
        {
            int window1Left = window1Rect.Left;
            int window1Top = window1Rect.Top;
            int window1Width = window1Rect.Width;
            int window1Height = window1Rect.Height;
            int offsetXInt = (int)offsetX;
            int offsetYInt = (int)offsetY;

            int window2X = 0, window2Y = 0;

            switch (placement)
            {
                case WindowPlacement.LeftTop:
                    window2X = window1Left - window2Width - offsetXInt;
                    window2Y = window1Top + offsetYInt;
                    break;
                case WindowPlacement.LeftCenter:
                    window2X = window1Left - window2Width - offsetXInt;
                    window2Y = window1Top + (window1Height - window2Height) / 2 + offsetYInt;
                    break;
                case WindowPlacement.LeftBottom:
                    window2X = window1Left - window2Width - offsetXInt;
                    window2Y = window1Top + window1Height - window2Height - offsetYInt;
                    break;
                case WindowPlacement.TopLeft:
                    window2X = window1Left + offsetXInt;
                    window2Y = window1Top - window2Height - offsetYInt;
                    break;
                case WindowPlacement.TopCenter:
                    window2X = window1Left + (window1Width - window2Width) / 2 + offsetXInt;
                    window2Y = window1Top - window2Height - offsetYInt;
                    break;
                case WindowPlacement.TopRight:
                    window2X = window1Left + window1Width - window2Width - offsetXInt;
                    window2Y = window1Top - window2Height - offsetYInt;
                    break;
                case WindowPlacement.RightTop:
                    window2X = window1Left + window1Width + offsetXInt;
                    window2Y = window1Top + offsetYInt;
                    break;
                case WindowPlacement.RightCenter:
                    window2X = window1Left + window1Width + offsetXInt;
                    window2Y = window1Top + (window1Height - window2Height) / 2 + offsetYInt;
                    break;
                case WindowPlacement.RightBottom:
                    window2X = window1Left + window1Width + offsetXInt;
                    window2Y = window1Top + window1Height - window2Height - offsetYInt;
                    break;
                case WindowPlacement.BottomLeft:
                    window2X = window1Left + offsetXInt;
                    window2Y = window1Top + window1Height + offsetYInt;
                    break;
                case WindowPlacement.BottomCenter:
                    window2X = window1Left + (window1Width - window2Width) / 2 + offsetXInt;
                    window2Y = window1Top + window1Height + offsetYInt;
                    break;
                case WindowPlacement.BottomRight:
                    window2X = window1Left + window1Width - window2Width - offsetXInt;
                    window2Y = window1Top + window1Height + offsetYInt;
                    break;
                default:
                    // Default to RightTop if unknown placement
                    window2X = window1Left + window1Width + offsetXInt;
                    window2Y = window1Top + offsetYInt;
                    break;
            }

            return (window2X, window2Y);
        }

        /// <summary>
        /// Calculate the Manhattan distance (L1 norm) between two points
        /// </summary>
        /// <param name="x1">First point X</param>
        /// <param name="y1">First point Y</param>
        /// <param name="x2">Second point X</param>
        /// <param name="y2">Second point Y</param>
        /// <returns>Manhattan distance (|dx| + |dy|)</returns>
        private static int CalculateManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
        }

        /// <summary>
        /// Find the nearest placement option based on current window positions
        /// </summary>
        /// <param name="window1Handle">Handle of window1</param>
        /// <param name="window2Handle">Handle of window2</param>
        /// <param name="offsetX">Horizontal offset</param>
        /// <param name="offsetY">Vertical offset</param>
        /// <returns>Best placement option that minimizes window2 movement</returns>
        internal static WindowPlacement FindNearestPlacement(IntPtr window1Handle, IntPtr window2Handle, 
            double offsetX = 0, double offsetY = 0)
        {
            // Get current window rectangles
            var window1Rect = WindowHelper.GetWindowRect(window1Handle);
            var window2Rect = WindowHelper.GetWindowRect(window2Handle);

            if (window1Rect == null || window2Rect == null)
            {
                // If we can't get window rects, default to RightTop
                return WindowPlacement.RightTop;
            }

            // Get current window2 position
            int currentWindow2X = window2Rect.Value.Left;
            int currentWindow2Y = window2Rect.Value.Top;
            int window2Width = window2Rect.Value.Width;
            int window2Height = window2Rect.Value.Height;

            // All possible placements (excluding Nearest itself)
            var allPlacements = new[]
            {
                WindowPlacement.LeftTop,
                WindowPlacement.LeftCenter,
                WindowPlacement.LeftBottom,
                WindowPlacement.TopLeft,
                WindowPlacement.TopCenter,
                WindowPlacement.TopRight,
                WindowPlacement.RightTop,
                WindowPlacement.RightCenter,
                WindowPlacement.RightBottom,
                WindowPlacement.BottomLeft,
                WindowPlacement.BottomCenter,
                WindowPlacement.BottomRight
            };

            // Find placement with minimum Manhattan distance
            var bestPlacement = allPlacements
                .Select(placement =>
                {
                    var (targetX, targetY) = CalculateTargetPosition(
                        window1Rect.Value, window2Width, window2Height, placement, offsetX, offsetY);
                    int distance = CalculateManhattanDistance(currentWindow2X, currentWindow2Y, targetX, targetY);
                    return new { Placement = placement, Distance = distance };
                })
                .OrderBy(x => x.Distance)
                .First()
                .Placement;

            return bestPlacement;
        }
    }
}

