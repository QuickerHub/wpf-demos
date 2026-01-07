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
        /// Enable edge hiding for a window with activation strategy (new API)
        /// This overload uses activationStrategy string parameter
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="animationType">Animation type string (None, Linear, EaseInOut). Default: "None"</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="activationStrategy">Window activation strategy string (AutoActivate, Topmost, None). Default: "AutoActivate"</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <param name="updateEdgeDirection">Edge direction for window restore/update string (Left, Top, Right, Bottom, Nearest, None). Default: "None"</param>
        /// <param name="showInTaskbar">If false, hide window from taskbar (default: true)</param>
        /// <param name="showDelay">Delay in milliseconds before showing window when mouse enters (default: 0, show immediately)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", string animationType = "None", bool showOnScreenEdge = false, bool autoUnregister = true, string activationStrategy = "AutoActivate", string quicker_param = "", string updateEdgeDirection = "None", bool showInTaskbar = true, int showDelay = 0)
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
                    if (hwnd == IntPtr.Zero)
                    {
                        result = new EnableEdgeHideResult
                        {
                            Success = false,
                            Message = "窗口句柄无效"
                        };
                        return;
                    }

                    // Get top-level window handle to prevent operations on child windows
                    // Operations on child windows are likely to fail
                    hwnd = WindowHelper.GetTopWindow(hwnd);
                    if (hwnd == IntPtr.Zero)
                    {
                        result = new EnableEdgeHideResult
                        {
                            Success = false,
                            Message = "无法获取顶层窗口句柄"
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
                    
                    // Handle autoUnregister: if enabled and window is already registered, unregister it
                    if (actualAutoUnregister && _services.ContainsKey(hwnd))
                    {
                        UnregisterEdgeHide(hwnd);
                        result = new EnableEdgeHideResult
                        {
                            Success = true,
                            Message = "贴边隐藏已取消"
                        };
                        return;
                    }
                    
                    // Handle quicker_param override for edgeDirection
                    string actualEdgeDirection = edgeDirection;
                    if (!string.IsNullOrEmpty(quicker_param))
                    {
                        actualEdgeDirection = quicker_param.ToLowerInvariant() switch
                        {
                            "left" => "Left",
                            "top" => "Top",
                            "right" => "Right",
                            "bottom" => "Bottom",
                            "auto" => "Nearest",
                            "none" => "None",
                            _ => edgeDirection // Unknown value, use original edgeDirection
                        };
                    }
                    
                    EdgeDirection direction = ParseEdgeDirection(actualEdgeDirection);
                    IntThickness thickness = ParseVisibleArea(visibleArea);
                    AnimationType animType = ParseAnimationType(animationType);
                    EdgeDirection updateDirection = ParseEdgeDirection(updateEdgeDirection);
                    ActivationStrategy activationStrategyEnum = ParseActivationStrategy(activationStrategy);
                    
                    var config = new EdgeHideConfig
                    {
                        WindowHandle = hwnd,
                        EdgeDirection = direction,
                        VisibleArea = thickness,
                        AnimationType = animType,
                        ShowOnScreenEdge = showOnScreenEdge,
                        ActivationStrategy = activationStrategyEnum,
                        UpdateEdgeDirection = updateDirection,
                        ShowInTaskbar = showInTaskbar,
                        ShowDelay = showDelay
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
        /// Enable edge hiding for a window with activation strategy (new API, compatibility overload with useAnimation boolean)
        /// This overload uses useAnimation boolean instead of animationType string for backward compatibility
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="useAnimation">If true, use EaseInOut animation; if false, no animation. Default: false</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="activationStrategy">Window activation strategy string (AutoActivate, Topmost, None). Default: "AutoActivate"</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <param name="updateEdgeDirection">Edge direction for window restore/update string (Left, Top, Right, Bottom, Nearest, None). Default: "None"</param>
        /// <param name="showInTaskbar">If false, hide window from taskbar (default: true)</param>
        /// <param name="showDelay">Delay in milliseconds before showing window when mouse enters (default: 0, show immediately)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", bool useAnimation = false, bool showOnScreenEdge = false, bool autoUnregister = true, string activationStrategy = "AutoActivate", string quicker_param = "", string updateEdgeDirection = "None", bool showInTaskbar = true, int showDelay = 0)
        {
            // Convert useAnimation boolean to animationType string
            string animationType = useAnimation ? "EaseInOut" : "None";
            return EnableEdgeHide(windowHandle, edgeDirection, visibleArea, animationType, showOnScreenEdge, autoUnregister, activationStrategy, quicker_param, updateEdgeDirection, showInTaskbar, showDelay);
        }

        /// <summary>
        /// Enable edge hiding for a window (legacy API with autoTopmost and useFocusAwareActivation)
        /// This overload is kept for backward compatibility
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="useAnimation">If true, use EaseInOut animation; if false, no animation. Default: false</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="autoTopmost">[Legacy] If true, automatically set window to topmost (default: true)</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <param name="updateEdgeDirection">Edge direction for window restore/update string (Left, Top, Right, Bottom, Nearest, None). Default: "None"</param>
        /// <param name="useFocusAwareActivation">[Legacy] If true, focused windows use auto-activate after show, unfocused windows use Topmost=true without activation (default: false)</param>
        /// <returns>Result object with success status and message</returns>
        [Obsolete("Use EnableEdgeHide with activationStrategy parameter instead. This method is kept for backward compatibility.")]
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", bool useAnimation = false, bool showOnScreenEdge = false, bool autoUnregister = true, bool autoTopmost = true, string quicker_param = "", string updateEdgeDirection = "None", bool useFocusAwareActivation = false)
        {
            // Convert useAnimation boolean to animationType string
            string animationType = useAnimation ? "EaseInOut" : "None";
            return EnableEdgeHide(windowHandle, edgeDirection, visibleArea, animationType, showOnScreenEdge, autoUnregister, autoTopmost, quicker_param, updateEdgeDirection, useFocusAwareActivation);
        }

        /// <summary>
        /// Enable edge hiding for a window (legacy API with autoTopmost and useFocusAwareActivation)
        /// This overload is kept for backward compatibility and forwards to the new activation strategy API
        /// </summary>
        /// <param name="windowHandle">Window handle as int</param>
        /// <param name="edgeDirection">Edge direction string (Left, Top, Right, Bottom, Nearest). Default: Nearest</param>
        /// <param name="visibleArea">Visible area thickness string: "5" (all sides), "5,6" (horizontal,vertical), or "1,2,3,4" (left,top,right,bottom). Default: "5"</param>
        /// <param name="animationType">Animation type string (None, Linear, EaseInOut). Default: "None"</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="autoUnregister">If true, second call will disable edge hiding (default: true)</param>
        /// <param name="autoTopmost">[Legacy] If true, automatically set window to topmost (default: true)</param>
        /// <param name="quicker_param">Quicker parameter to override edgeDirection: "left", "top", "right", "bottom", "auto", or empty string (default: empty string)</param>
        /// <param name="updateEdgeDirection">Edge direction for window restore/update string (Left, Top, Right, Bottom, Nearest, None). Default: "None"</param>
        /// <param name="useFocusAwareActivation">[Legacy] If true, focused windows use auto-activate after show, unfocused windows use Topmost=true without activation (default: false)</param>
        /// <returns>Result object with success status and message</returns>
        [Obsolete("Use EnableEdgeHide with activationStrategy parameter instead. This method is kept for backward compatibility.")]
        public static EnableEdgeHideResult EnableEdgeHide(int windowHandle, string edgeDirection = "Nearest", 
            string visibleArea = "5", string animationType = "None", bool showOnScreenEdge = false, bool autoUnregister = true, bool autoTopmost = true, string quicker_param = "", string updateEdgeDirection = "None", bool useFocusAwareActivation = false)
        {
            // Convert legacy parameters to new ActivationStrategy
            ActivationStrategy activationStrategy = ConvertLegacyActivationParams(autoTopmost, useFocusAwareActivation);
            string activationStrategyString = activationStrategy.ToString();
            
            // Forward to new API with activation strategy
            return EnableEdgeHide(windowHandle, edgeDirection, visibleArea, animationType, showOnScreenEdge, autoUnregister, activationStrategyString, quicker_param, updateEdgeDirection);
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

            // Note: GetTopWindow should already be called by the calling method
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

            // Convert legacy config format if needed
            ConvertLegacyConfig(config);

            // Handle taskbar visibility
            if (!config.ShowInTaskbar)
            {
                // Record original taskbar visibility state
                config.OriginalTaskbarVisible = WindowHelper.IsWindowVisibleInTaskbar(config.WindowHandle);
                
                // Hide window from taskbar
                WindowHelper.SetWindowTaskbarVisibility(config.WindowHandle, false);
            }

            // Create new service (constructor initializes everything)
            var service = new WindowEdgeHideService(config.WindowHandle, config.EdgeDirection, config.VisibleArea, mover, config.ShowOnScreenEdge, config.ActivationStrategy, config.UpdateEdgeDirection, config.ShowDelay);
            service.WindowDestroyed += (hwnd) =>
            {
                // Restore taskbar visibility if needed
                if (_configs.TryGetValue(hwnd, out var destroyedConfig) && !destroyedConfig.ShowInTaskbar)
                {
                    WindowHelper.SetWindowTaskbarVisibility(hwnd, destroyedConfig.OriginalTaskbarVisible);
                }
                
                _services.Remove(hwnd);
                _configs.Remove(hwnd);
                SaveConfigs();
            };

            try
            {
                _services[config.WindowHandle] = service;
                _configs[config.WindowHandle] = config;
                SaveConfigs();
                
                string directionText = config.EdgeDirection switch
                {
                    EdgeDirection.Nearest => "最近边缘",
                    EdgeDirection.None => "无（仅注册）",
                    _ => config.EdgeDirection.ToString()
                };
                string animationText = config.AnimationType switch
                {
                    AnimationType.None => "未启用动画",
                    AnimationType.Linear => "已启用线性动画",
                    AnimationType.EaseInOut => "已启用缓入缓出动画",
                    _ => "未启用动画"
                };
                string taskbarText = !config.ShowInTaskbar ? "，已隐藏任务栏" : "";
                string message = wasEnabled 
                    ? $"贴边隐藏已重新启用 - 方向: {directionText}, {animationText}{taskbarText}"
                    : $"贴边隐藏已启用 - 方向: {directionText}, {animationText}{taskbarText}";
                
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
        /// <param name="updateEdgeDirection">Edge direction for window restore/update. If None, automatically selects nearest edge (default: None)</param>
        /// <param name="useFocusAwareActivation">If true, focused windows use auto-activate after show, unfocused windows use Topmost=true without activation (default: false)</param>
        /// <param name="showDelay">Delay in milliseconds before showing window when mouse enters (default: 0, show immediately)</param>
        /// <returns>Result object with success status and message</returns>
        public static EnableEdgeHideResult EnableEdgeHide(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest,
            IntThickness visibleArea = default, AnimationType animationType = AnimationType.None, bool showOnScreenEdge = false, bool autoTopmost = true, EdgeDirection updateEdgeDirection = EdgeDirection.None, bool useFocusAwareActivation = false, int showDelay = 0)
        {
            // Get top-level window handle to prevent operations on child windows
            // Operations on child windows are likely to fail
            windowHandle = WindowHelper.GetTopWindow(windowHandle);
            if (windowHandle == IntPtr.Zero)
            {
                return new EnableEdgeHideResult
                {
                    Success = false,
                    Message = "无法获取顶层窗口句柄"
                };
            }

            // Convert legacy parameters to new ActivationStrategy
            ActivationStrategy activationStrategy = ConvertLegacyActivationParams(autoTopmost, useFocusAwareActivation);

            var config = new EdgeHideConfig
            {
                WindowHandle = windowHandle,
                EdgeDirection = edgeDirection,
                VisibleArea = visibleArea.Equals(default(IntThickness)) ? new IntThickness(5) : visibleArea,
                AnimationType = animationType,
                ShowOnScreenEdge = showOnScreenEdge,
                ActivationStrategy = activationStrategy,
                UpdateEdgeDirection = updateEdgeDirection,
                ShowDelay = showDelay
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
                "none" => EdgeDirection.None,
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
        /// Parse activation strategy string to enum
        /// </summary>
        /// <param name="activationStrategy">Activation strategy string (AutoActivate, Topmost, None)</param>
        /// <returns>ActivationStrategy enum value</returns>
        private static ActivationStrategy ParseActivationStrategy(string activationStrategy)
        {
            if (string.IsNullOrWhiteSpace(activationStrategy))
                return ActivationStrategy.AutoActivate;

            if (Enum.TryParse<ActivationStrategy>(activationStrategy.Trim(), ignoreCase: true, out var result))
                return result;

            return ActivationStrategy.AutoActivate;
        }

        /// <summary>
        /// Convert legacy autoTopmost and useFocusAwareActivation parameters to ActivationStrategy
        /// </summary>
        /// <param name="autoTopmost">Legacy parameter: whether to automatically set window to topmost</param>
        /// <param name="useFocusAwareActivation">Legacy parameter: whether to use focus-aware activation</param>
        /// <returns>ActivationStrategy enum value</returns>
        private static ActivationStrategy ConvertLegacyActivationParams(bool autoTopmost, bool useFocusAwareActivation)
        {
            // Priority: useFocusAwareActivation > autoTopmost
            if (useFocusAwareActivation)
            {
                return ActivationStrategy.AutoActivate;
            }
            else if (autoTopmost)
            {
                return ActivationStrategy.Topmost;
            }
            else
            {
                return ActivationStrategy.None;
            }
        }

        /// <summary>
        /// Convert EdgeHideConfig from legacy format (AutoTopmost, UseFocusAwareActivation) to new format (ActivationStrategy)
        /// </summary>
        /// <param name="config">Configuration to convert</param>
        private static void ConvertLegacyConfig(EdgeHideConfig config)
        {
            // If ActivationStrategy is default (AutoActivate) and legacy properties are set,
            // convert from legacy properties to new format
            // This handles both cases:
            // 1. New configs with ActivationStrategy explicitly set (won't convert)
            // 2. Legacy configs loaded from JSON (will convert)
            if (config.ActivationStrategy == ActivationStrategy.AutoActivate)
            {
                // Check if legacy properties indicate a different strategy
                // If UseFocusAwareActivation is true, it maps to AutoActivate (already default, no change needed)
                // If AutoTopmost is false and UseFocusAwareActivation is false, it maps to None
                // If AutoTopmost is true and UseFocusAwareActivation is false, it maps to Topmost
                if (!config.UseFocusAwareActivation)
                {
                    if (!config.AutoTopmost)
                    {
                        config.ActivationStrategy = ActivationStrategy.None;
                    }
                    else
                    {
                        config.ActivationStrategy = ActivationStrategy.Topmost;
                    }
                }
                // If UseFocusAwareActivation is true, ActivationStrategy.AutoActivate is correct (default)
            }
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
                // Restore taskbar visibility if needed
                if (_configs.TryGetValue(windowHandle, out var config) && !config.ShowInTaskbar)
                {
                    WindowHelper.SetWindowTaskbarVisibility(windowHandle, config.OriginalTaskbarVisible);
                }
                
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
            
            // Get all window handles before clearing
            var windowHandles = new List<IntPtr>(_services.Keys);
            
            // Unregister each window (this handles taskbar visibility, dispose, and removal)
            foreach (var windowHandle in windowHandles)
            {
                if (UnregisterEdgeHide(windowHandle))
                {
                    count++;
                }
            }
            
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

