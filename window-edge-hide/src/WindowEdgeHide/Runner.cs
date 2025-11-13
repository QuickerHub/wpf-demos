using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using WindowEdgeHide.Interfaces;
using WindowEdgeHide.Models;
using WindowEdgeHide.Services;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class Runner
    {
        private static readonly Dictionary<IntPtr, WindowEdgeHideService> _services = new Dictionary<IntPtr, WindowEdgeHideService>();

        /// <summary>
        /// Enable edge hiding for a window
        /// This overload supports int handle and string visibleArea for Quicker integration
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="useAnimation">Whether to use animation for window movement (default: false)</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <returns>True if enabled successfully</returns>
        public static bool EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", bool useAnimation = false, bool showOnScreenEdge = false)
        {
            IntPtr hwnd = new IntPtr(windowHandle);
            EdgeDirection direction = ParseEdgeDirection(edgeDirection);
            IntThickness thickness = ParseVisibleArea(visibleArea);
            
            // Ensure execution on UI thread
            bool result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                result = EnableEdgeHide(hwnd, direction, thickness, useAnimation, showOnScreenEdge);
            });
            return result;
        }

        /// <summary>
        /// Enable edge hiding for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edgeDirection">Edge direction. If Nearest, automatically selects nearest edge.</param>
        /// <param name="visibleArea">Visible area thickness when hidden (default: all sides 5)</param>
        /// <param name="useAnimation">Whether to use animation for window movement (default: false)</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <returns>True if enabled successfully</returns>
        public static bool EnableEdgeHide(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest,
            IntThickness visibleArea = default, bool useAnimation = false, bool showOnScreenEdge = false)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle cannot be zero");
            }

            // Disable existing service if any
            DisableEdgeHide(windowHandle);

            // Create window mover based on animation setting
            // Use single mover for both hide and show to prevent animation conflicts
            IWindowMover? mover = null;
            
            if (useAnimation)
            {
                mover = new Implementations.AnimatedWindowMover();
            }

            // Create and enable new service
            var service = new WindowEdgeHideService();
            service.WindowDestroyed += (hwnd) =>
            {
                _services.Remove(hwnd);
            };

            try
            {
                service.Enable(windowHandle, edgeDirection, visibleArea, mover, showOnScreenEdge);
                _services[windowHandle] = service;
                return true;
            }
            catch
            {
                service.Dispose();
                return false;
            }
        }

        /// <summary>
        /// Parse edge direction string to enum
        /// </summary>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest)</param>
        /// <returns>EdgeDirection enum value</returns>
        private static EdgeDirection ParseEdgeDirection(string edgeDirection)
        {
            if (string.IsNullOrWhiteSpace(edgeDirection))
                return EdgeDirection.Nearest;

            return edgeDirection.Trim().ToLower() switch
            {
                "left" => EdgeDirection.Left,
                "top" => EdgeDirection.Top,
                "right" => EdgeDirection.Right,
                "bottom" => EdgeDirection.Bottom,
                "nearest" => EdgeDirection.Nearest,
                _ => EdgeDirection.Nearest
            };
        }

        /// <summary>
        /// Set window mover for animation for a specific window (used for both hiding and showing)
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="mover">Window mover implementation</param>
        /// <returns>True if set successfully</returns>
        public static bool SetMover(IntPtr windowHandle, IWindowMover mover)
        {
            if (_services.TryGetValue(windowHandle, out var service))
            {
                service.SetMover(mover);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Disable edge hiding for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <returns>True if disabled successfully</returns>
        public static bool DisableEdgeHide(IntPtr windowHandle)
        {
            if (_services.TryGetValue(windowHandle, out var service))
            {
                service.Dispose();
                _services.Remove(windowHandle);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Disable edge hiding for a window (int handle overload)
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <returns>True if disabled successfully</returns>
        public static bool DisableEdgeHide(int windowHandle)
        {
            return DisableEdgeHide(new IntPtr(windowHandle));
        }

        /// <summary>
        /// Check if edge hiding is enabled for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <returns>True if enabled</returns>
        public static bool IsEnabled(IntPtr windowHandle)
        {
            return _services.ContainsKey(windowHandle);
        }

        /// <summary>
        /// Check if edge hiding is enabled for a window (int handle overload)
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <returns>True if enabled</returns>
        public static bool IsEnabled(int windowHandle)
        {
            return IsEnabled(new IntPtr(windowHandle));
        }

        /// <summary>
        /// Disable edge hiding for all windows
        /// </summary>
        public static void DisableAll()
        {
            foreach (var service in _services.Values)
            {
                service.Dispose();
            }
            _services.Clear();
        }

        /// <summary>
        /// Parse visible area thickness string to IntThickness struct
        /// </summary>
        /// <param name="visibleArea">Visible area thickness string</param>
        /// <returns>IntThickness struct</returns>
        private static IntThickness ParseVisibleArea(string visibleArea)
        {
            try
            {
                return IntThickness.Parse(visibleArea);
            }
            catch (FormatException)
            {
                // Log error or use a default value
                Console.WriteLine($"Warning: Invalid visible area format '{visibleArea}'. Using default of 5.");
                return new IntThickness(5); // Default to 5 if parsing fails
            }
        }
    }
}

