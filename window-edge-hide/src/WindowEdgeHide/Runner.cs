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
    /// Result of EnableEdgeHide operation
    /// </summary>
    public class EnableEdgeHideResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

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
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", bool useAnimation = false, bool showOnScreenEdge = false, bool autoUnregister = true)
        {
            // Ensure entire method executes on UI thread
            EnableEdgeHideResult? result = null;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IntPtr hwnd = new IntPtr(windowHandle);
                    
                    // Check if window handle is valid
                    if (hwnd == IntPtr.Zero)
                    {
                        result = new EnableEdgeHideResult
                        {
                            Success = false,
                            Message = "窗口句柄无效"
                        };
                        return;
                    }

                    // Check if it's a special system window (desktop, taskbar, etc.)
                    if (WindowHelper.IsSpecialSystemWindow(hwnd))
                    {
                        string className = WindowHelper.GetWindowClassName(hwnd);
                        string windowType = className switch
                        {
                            "Progman" or "WorkerW" => "桌面",
                            "Shell_TrayWnd" => "任务栏",
                            _ => "系统窗口"
                        };
                        result = new EnableEdgeHideResult
                        {
                            Success = false,
                            Message = $"不支持对{windowType}窗口启用贴边隐藏"
                        };
                        return;
                    }
                    
                    // Check if already enabled and autoUnregister is true
                    if (autoUnregister && IsEnabled(hwnd))
                    {
                        // Unregister edge hiding
                        bool unregistered = UnregisterEdgeHide(hwnd);
                        result = new EnableEdgeHideResult
                        {
                            Success = unregistered,
                            Message = unregistered ? "贴边隐藏已取消" : "取消贴边隐藏失败"
                        };
                        return;
                    }
                    
                    EdgeDirection direction = ParseEdgeDirection(edgeDirection);
                    IntThickness thickness = ParseVisibleArea(visibleArea);
                    
                    result = EnableEdgeHide(hwnd, direction, thickness, useAnimation, showOnScreenEdge);
                });
            }
            catch (Exception ex)
            {
                return new EnableEdgeHideResult
                {
                    Success = false,
                    Message = $"执行失败: {ex.Message}"
                };
            }
            
            return result ?? new EnableEdgeHideResult { Success = false, Message = "执行失败" };
        }

        /// <summary>
        /// Enable edge hiding for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edgeDirection">Edge direction. If Nearest, automatically selects nearest edge.</param>
        /// <param name="visibleArea">Visible area thickness when hidden (default: all sides 5)</param>
        /// <param name="useAnimation">Whether to use animation for window movement (default: false)</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest,
            IntThickness visibleArea = default, bool useAnimation = false, bool showOnScreenEdge = false)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return new EnableEdgeHideResult
                {
                    Success = false,
                    Message = "窗口句柄无效"
                };
            }

            // Unregister existing service if any
            bool wasEnabled = UnregisterEdgeHide(windowHandle);

            // Create window mover based on animation setting
            // Use single mover for both hide and show to prevent animation conflicts
            IWindowMover? mover = null;
            
            if (useAnimation)
            {
                mover = new Implementations.AnimatedWindowMover();
            }

            // Create new service (constructor initializes everything)
            var service = new WindowEdgeHideService(windowHandle, edgeDirection, visibleArea, mover, showOnScreenEdge);
            service.WindowDestroyed += (hwnd) =>
            {
                _services.Remove(hwnd);
            };

            try
            {
                _services[windowHandle] = service;
                
                string directionText = edgeDirection == EdgeDirection.Nearest ? "最近边缘" : edgeDirection.ToString();
                string animationText = useAnimation ? "已启用动画" : "未启用动画";
                string message = wasEnabled 
                    ? $"贴边隐藏已重新启用 - 方向: {directionText}, {animationText}"
                    : $"贴边隐藏已启用 - 方向: {directionText}, {animationText}";
                
                return new EnableEdgeHideResult
                {
                    Success = true,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                service.Dispose();
                return new EnableEdgeHideResult
                {
                    Success = false,
                    Message = $"启用贴边隐藏失败: {ex.Message}"
                };
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
        /// Unregister edge hiding for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <returns>True if unregistered successfully</returns>
        public static bool UnregisterEdgeHide(IntPtr windowHandle)
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
        /// Unregister edge hiding for a window (int handle overload)
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <returns>True if unregistered successfully</returns>
        public static bool UnregisterEdgeHide(int windowHandle)
        {
            return UnregisterEdgeHide(new IntPtr(windowHandle));
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
        /// Unregister edge hiding for all windows
        /// </summary>
        public static void UnregisterAll()
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

