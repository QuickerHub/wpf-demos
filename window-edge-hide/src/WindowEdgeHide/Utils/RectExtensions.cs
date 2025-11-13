using System;
using WindowEdgeHide.Models;

namespace WindowEdgeHide.Utils
{
    /// <summary>
    /// Extension methods for WindowRect calculations
    /// </summary>
    internal static class RectExtensions
    {
        /// <summary>
        /// Check if a point is within the rectangle
        /// </summary>
        public static bool Contains(this WindowRect rect, (int x, int y) point)
        {
            return point.x >= rect.Left && point.x <= rect.Right &&
                   point.y >= rect.Top && point.y <= rect.Bottom;
        }

        /// <summary>
        /// Check if a point is near the rectangle (within threshold distance)
        /// </summary>
        public static bool IsNear(this WindowRect rect, (int x, int y) point, int threshold)
        {
            // Expand rectangle by threshold and check if point is within
            return point.x >= rect.Left - threshold && point.x <= rect.Right + threshold &&
                   point.y >= rect.Top - threshold && point.y <= rect.Bottom + threshold;
        }

        /// <summary>
        /// Calculate intersection of two rectangles
        /// Treats rectangles as two line segments (x and y dimensions)
        /// For each dimension, calculates intersection; if no intersection, returns midpoint
        /// Then constructs rectangle from the two one-dimensional regions
        /// </summary>
        public static WindowRect Intersect(this WindowRect rect1, WindowRect rect2)
        {
            // X dimension: treat as line segment [Left, Right]
            int xLeft = Math.Max(rect1.Left, rect2.Left);
            int xRight = Math.Min(rect1.Right, rect2.Right);
            int resultLeft, resultRight;
            
            if (xLeft < xRight)
            {
                // X segments intersect
                resultLeft = xLeft;
                resultRight = xRight;
            }
            else
            {
                // X segments don't intersect, return midpoint between nearest boundaries
                if (rect1.Right <= rect2.Left)
                {
                    // rect1 is to the left of rect2, midpoint between rect1.Right and rect2.Left
                    int midX = (rect1.Right + rect2.Left) / 2;
                    resultLeft = midX;
                    resultRight = midX;
                }
                else // rect2.Right <= rect1.Left
                {
                    // rect2 is to the left of rect1, midpoint between rect2.Right and rect1.Left
                    int midX = (rect2.Right + rect1.Left) / 2;
                    resultLeft = midX;
                    resultRight = midX;
                }
            }
            
            // Y dimension: treat as line segment [Top, Bottom]
            int yTop = Math.Max(rect1.Top, rect2.Top);
            int yBottom = Math.Min(rect1.Bottom, rect2.Bottom);
            int resultTop, resultBottom;
            
            if (yTop < yBottom)
            {
                // Y segments intersect
                resultTop = yTop;
                resultBottom = yBottom;
            }
            else
            {
                // Y segments don't intersect, return midpoint between nearest boundaries
                if (rect1.Bottom <= rect2.Top)
                {
                    // rect1 is above rect2, midpoint between rect1.Bottom and rect2.Top
                    int midY = (rect1.Bottom + rect2.Top) / 2;
                    resultTop = midY;
                    resultBottom = midY;
                }
                else // rect2.Bottom <= rect1.Top
                {
                    // rect2 is above rect1, midpoint between rect2.Bottom and rect1.Top
                    int midY = (rect2.Bottom + rect1.Top) / 2;
                    resultTop = midY;
                    resultBottom = midY;
                }
            }
            
            return new WindowRect(resultLeft, resultTop, resultRight, resultBottom);
        }

        /// <summary>
        /// Shrink rectangle by thickness (positive values shrink inward, negative values expand outward)
        /// Does not validate rectangle bounds - allows invalid rectangles to be returned
        /// </summary>
        public static WindowRect Shrink(this WindowRect rect, IntThickness thickness)
        {
            int left = rect.Left + thickness.Left;
            int top = rect.Top + thickness.Top;
            int right = rect.Right - thickness.Right;
            int bottom = rect.Bottom - thickness.Bottom;

            return new WindowRect(left, top, right, bottom);
        }

        /// <summary>
        /// Expand rectangle by a uniform value (positive values expand outward, negative values shrink inward)
        /// </summary>
        public static WindowRect Expand(this WindowRect rect, int value)
        {
            return new WindowRect(
                rect.Left - value,
                rect.Top - value,
                rect.Right + value,
                rect.Bottom + value
            );
        }

        /// <summary>
        /// Calculate the distance between two rectangles (distance between nearest edges)
        /// Returns 0 if rectangles intersect or overlap
        /// </summary>
        /// <param name="rect1">First rectangle</param>
        /// <param name="rect2">Second rectangle</param>
        /// <returns>Distance between nearest edges, or 0 if rectangles intersect</returns>
        public static int Distance(this WindowRect rect1, WindowRect rect2)
        {
            // Check if rectangles intersect (distance is 0)
            if (rect1.Left <= rect2.Right && rect1.Right >= rect2.Left &&
                rect1.Top <= rect2.Bottom && rect1.Bottom >= rect2.Top)
            {
                return 0;
            }

            // Calculate horizontal distance
            int horizontalDistance = 0;
            if (rect1.Right < rect2.Left)
            {
                // rect1 is to the left of rect2
                horizontalDistance = rect2.Left - rect1.Right;
            }
            else if (rect2.Right < rect1.Left)
            {
                // rect2 is to the left of rect1
                horizontalDistance = rect1.Left - rect2.Right;
            }

            // Calculate vertical distance
            int verticalDistance = 0;
            if (rect1.Bottom < rect2.Top)
            {
                // rect1 is above rect2
                verticalDistance = rect2.Top - rect1.Bottom;
            }
            else if (rect2.Bottom < rect1.Top)
            {
                // rect2 is above rect1
                verticalDistance = rect1.Top - rect2.Bottom;
            }

            // Return the distance between nearest edges (Manhattan distance)
            return horizontalDistance + verticalDistance;
        }

        /// <summary>
        /// Check if rectangle is at any edge of screen and return which edges
        /// </summary>
        public static void CheckEdges(this WindowRect rect, WindowRect screen, int threshold,
            out bool atLeft, out bool atTop, out bool atRight, out bool atBottom)
        {
            atLeft = rect.Left <= screen.Left + threshold;
            atTop = rect.Top <= screen.Top + threshold;
            atRight = rect.Right >= screen.Right - threshold;
            atBottom = rect.Bottom >= screen.Bottom - threshold;
        }

        /// <summary>
        /// Check if a point is near any screen edge and return which edges
        /// </summary>
        public static void CheckNearEdges(this (int x, int y) point, WindowRect screen, int threshold,
            out bool nearLeft, out bool nearTop, out bool nearRight, out bool nearBottom)
        {
            nearLeft = point.x <= screen.Left + threshold;
            nearTop = point.y <= screen.Top + threshold;
            nearRight = point.x >= screen.Right - threshold;
            nearBottom = point.y >= screen.Bottom - threshold;
        }
    }
}

