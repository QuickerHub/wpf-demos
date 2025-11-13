using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
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
        private static readonly Dictionary<IntPtr, EdgeHideConfig> _configs = new Dictionary<IntPtr, EdgeHideConfig>();
        private static readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowEdgeHide",
            "config.json");


        /// <summary>
        /// Static constructor to load saved configurations and register cleanup on application exit
        /// For injected DLLs, ProcessExit is the most reliable event
        /// </summary>
        static Runner()
        {
            // Load saved configurations
            LoadConfigs();
        }

        /// <summary>
        /// Enable edge hiding for a window (compatibility overload with useAnimation boolean)
        /// This overload uses useAnimation boolean instead of animationType string for backward compatibility
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="useAnimation">If true, use EaseInOut animation; if false, no animation. Default: false</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="autoTopmost">If true, automatically set window to topmost (default: true)</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", bool useAnimation = false, bool showOnScreenEdge = false, bool autoUnregister = true, bool autoTopmost = true, string quicker_param = "")
        {
            // Convert useAnimation boolean to animationType string
            string animationType = useAnimation ? "EaseInOut" : "None";
            return EnableEdgeHide(windowHandle, edgeDirection, visibleArea, animationType, showOnScreenEdge, autoUnregister, autoTopmost, quicker_param);
        }

        /// <summary>
        /// Enable edge hiding for a window
        /// This overload supports int handle and string visibleArea for Quicker integration
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="animationType">Animation type string (None, Linear, EaseInOut). Default: "None"</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="autoTopmost">If true, automatically set window to topmost (default: true)</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", string animationType = "None", bool showOnScreenEdge = false, bool autoUnregister = true, bool autoTopmost = true, string quicker_param = "")
        {
            // Ensure entire method executes on UI thread
            EnableEdgeHideResult? result = null;
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Handle special commands first (these don't need window handle)
                    if (!string.IsNullOrEmpty(quicker_param))
                    {
                        string cmd = quicker_param.ToLowerInvariant();
                        
                        // Handle "manage" command - open management window
                        if (cmd == "manage")
                        {
                            OpenManagementWindow();
                            result = new EnableEdgeHideResult
                            {
                                Success = true,
                                Message = "管理窗口已打开"
                            };
                            return;
                        }
                        
                        // Handle "stopall" command - unregister all windows
                        if (cmd == "stopall")
                        {
                            int count = UnregisterAll();
                            result = new EnableEdgeHideResult
                            {
                                Success = true,
                                Message = $"已取消 {count} 个窗口的贴边隐藏"
                            };
                            return;
                        }
                    }
                    
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
                    
                    // When using command (quicker_param provided), force autoUnregister = false
                    bool actualAutoUnregister = autoUnregister;
                    if (!string.IsNullOrEmpty(quicker_param))
                    {
                        actualAutoUnregister = false;
                    }
                    
                    // Check if already enabled and autoUnregister is true
                    if (actualAutoUnregister && IsEnabled(hwnd))
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
                    
                    // Override edgeDirection with quicker_param if provided
                    string actualEdgeDirection = edgeDirection;
                    if (!string.IsNullOrEmpty(quicker_param))
                    {
                        // Map quicker_param to edgeDirection
                        actualEdgeDirection = quicker_param.ToLowerInvariant() switch
                        {
                            "left" => "Left",
                            "top" => "Top",
                            "right" => "Right",
                            "bottom" => "Bottom",
                            "auto" => "Nearest",
                            _ => edgeDirection // Unknown value, use original edgeDirection
                        };
                    }
                    
                    EdgeDirection direction = ParseEdgeDirection(actualEdgeDirection);
                    IntThickness thickness = ParseVisibleArea(visibleArea);
                    AnimationType animType = ParseAnimationType(animationType);
                    
                    var config = new EdgeHideConfig
                    {
                        WindowHandle = hwnd,
                        EdgeDirection = direction,
                        VisibleArea = thickness,
                        AnimationType = animType,
                        ShowOnScreenEdge = showOnScreenEdge,
                        AutoTopmost = autoTopmost
                    };
                    result = EnableEdgeHide(config);
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
        /// Enable edge hiding for a window using configuration
        /// </summary>
        /// <param name="config">Edge hide configuration</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(EdgeHideConfig config)
        {
            if (config.WindowHandle == IntPtr.Zero)
            {
                return new EnableEdgeHideResult
                {
                    Success = false,
                    Message = "窗口句柄无效"
                };
            }

            // Unregister existing service if any
            bool wasEnabled = UnregisterEdgeHide(config.WindowHandle);

            // Create window mover based on animation type
            // Use single mover for both hide and show to prevent animation conflicts
            IWindowMover? mover = null;
            
            if (config.AnimationType == AnimationType.Linear)
            {
                mover = new Implementations.AnimatedWindowMover();
            }
            else if (config.AnimationType == AnimationType.EaseInOut)
            {
                mover = new Implementations.EaseInOutWindowMover();
            }

            // Create new service (constructor initializes everything)
            var service = new WindowEdgeHideService(config.WindowHandle, config.EdgeDirection, config.VisibleArea, mover, config.ShowOnScreenEdge, config.AutoTopmost);
            service.WindowDestroyed += (hwnd) =>
            {
                _services.Remove(hwnd);
                _configs.Remove(hwnd);
                SaveConfigs();
            };

            try
            {
                _services[config.WindowHandle] = service;
                _configs[config.WindowHandle] = config;
                SaveConfigs();
                
                string directionText = config.EdgeDirection == EdgeDirection.Nearest ? "最近边缘" : config.EdgeDirection.ToString();
                string animationText = config.AnimationType switch
                {
                    AnimationType.None => "未启用动画",
                    AnimationType.Linear => "已启用线性动画",
                    AnimationType.EaseInOut => "已启用缓入缓出动画",
                    _ => "未启用动画"
                };
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
        /// Enable edge hiding for a window (legacy overload)
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edgeDirection">Edge direction. If Nearest, automatically selects nearest edge.</param>
        /// <param name="visibleArea">Visible area thickness when hidden (default: all sides 5)</param>
        /// <param name="animationType">Animation type for window movement (default: None)</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoTopmost">If true, automatically set window to topmost (default: true)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest,
            IntThickness visibleArea = default, AnimationType animationType = AnimationType.None, bool showOnScreenEdge = false, bool autoTopmost = true)
        {
            var config = new EdgeHideConfig
            {
                WindowHandle = windowHandle,
                EdgeDirection = edgeDirection,
                VisibleArea = visibleArea.Equals(default(IntThickness)) ? new IntThickness(5) : visibleArea,
                AnimationType = animationType,
                ShowOnScreenEdge = showOnScreenEdge,
                AutoTopmost = autoTopmost
            };
            return EnableEdgeHide(config);
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
        /// Parse animation type string to enum
        /// </summary>
        /// <param name="animationType">Animation type string (None, Linear, EaseInOut)</param>
        /// <returns>AnimationType enum value</returns>
        private static AnimationType ParseAnimationType(string animationType)
        {
            if (string.IsNullOrWhiteSpace(animationType))
                return AnimationType.None;

            if (Enum.TryParse<AnimationType>(animationType.Trim(), ignoreCase: true, out var result))
                return result;

            return AnimationType.None;
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
                _configs.Remove(windowHandle);
                SaveConfigs();
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
        public static int UnregisterAll()
        {
            int count = 0;
            foreach (var service in _services.Values)
            {
                service.Dispose();
                count++;
            }
            _services.Clear();
            return count;
        }

        /// <summary>
        /// Get all registered window handles
        /// </summary>
        /// <returns>List of registered window handles</returns>
        public static List<IntPtr> GetRegisteredWindows()
        {
            return new List<IntPtr>(_services.Keys);
        }

        /// <summary>
        /// Open management window
        /// </summary>
        private static void OpenManagementWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new ManagementWindow();
                window.Show();
            });
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

        /// <summary>
        /// Save configurations to JSON file
        /// </summary>
        private static void SaveConfigs()
        {
            try
            {
                var configsList = _configs.Values.ToList();
                string json = JsonConvert.SerializeObject(configsList, Formatting.Indented);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_configFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        /// <summary>
        /// Load configurations from JSON file and restore registrations
        /// </summary>
        public static void LoadConfigs()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return;

                string json = File.ReadAllText(_configFilePath);
                var configs = JsonConvert.DeserializeObject<List<EdgeHideConfig>>(json);
                
                if (configs == null)
                    return;

                foreach (var config in configs)
                {
                    // Check if window still exists and is valid
                    if (config.WindowHandle == IntPtr.Zero)
                        continue;

                    var hwnd = new Windows.Win32.Foundation.HWND(config.WindowHandle);
                    if (!Windows.Win32.PInvoke.IsWindow(hwnd))
                        continue;

                    // Check if it's a special system window
                    if (WindowHelper.IsSpecialSystemWindow(config.WindowHandle))
                        continue;

                    // Try to register
                    try
                    {
                        EnableEdgeHide(config);
                    }
                    catch
                    {
                        // Skip invalid windows
                    }
                }
            }
            catch
            {
                // Ignore load errors
            }
        }
    }
}

