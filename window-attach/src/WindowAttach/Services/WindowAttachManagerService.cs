using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowAttach.Models;
using WindowAttach.Utils;
using WindowAttach.Views;

namespace WindowAttach.Services
{
    /// <summary>
    /// Service for managing multiple window attachments
    /// </summary>
    public class WindowAttachManagerService : IDisposable
    {
        private readonly Dictionary<string, WindowAttachPair> _pairsCache = new Dictionary<string, WindowAttachPair>();
        private readonly Dictionary<string, WindowAttachService> _attachments = new Dictionary<string, WindowAttachService>();
        private readonly Dictionary<string, DetachPopupWindow> _popupWindows = new Dictionary<string, DetachPopupWindow>();
        private readonly Dictionary<string, (IntPtr mainWindow1Handle, IntPtr mainWindow2Handle)> _popupToMainMapping = new Dictionary<string, (IntPtr, IntPtr)>(); // Map popup key to main attachment
        private readonly HashSet<IntPtr> _blacklist = new HashSet<IntPtr>(); // Blacklist for window handles that should not be attached

        /// <summary>
        /// Get all window pairs (Main attachments only)
        /// </summary>
        public IEnumerable<WindowAttachPair> GetMainPairs()
        {
            return _pairsCache.Values.Where(pair => pair.AttachType == AttachType.Main).ToList();
        }

        /// <summary>
        /// Register a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window (window to follow)</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows)</param>
        /// <param name="placement">Placement position</param>
        /// <param name="offsetX">Horizontal offset</param>
        /// <param name="offsetY">Vertical offset</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1</param>
        /// <param name="autoAdjustToScreen">Whether to automatically adjust position to maximize visible area when window is not fully visible</param>
        /// <param name="attachType">Type of attachment (Main or Popup)</param>
        /// <param name="callbackAction">Callback action to execute when window1 is closed (default: null)</param>
        /// <returns>True if registered successfully, false if already registered</returns>
        public bool Register(IntPtr window1Handle, IntPtr window2Handle, WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false, AttachType attachType = AttachType.Main, Action? callbackAction = null)
        {
            // Check if window1 and window2 are the same
            if (window1Handle == window2Handle)
            {
                throw new ArgumentException("Window1 and Window2 cannot be the same window");
            }

            // Blacklist check: only apply to Main attachments
            // Popup attachments should not be blocked by blacklist (popup can be attached as window2)
            if (attachType == AttachType.Main)
            {
                // For Main attachments, check if either window is in the blacklist
                if (_blacklist.Contains(window1Handle) || _blacklist.Contains(window2Handle))
                {
                    return false;
                }
            }
            // For Popup attachments, skip blacklist check (allow popup to be attached)

            // Handle Nearest placement: calculate the best placement based on current window positions
            WindowPlacement actualPlacement = placement;
            if (placement == WindowPlacement.Nearest)
            {
                actualPlacement = PlacementCalculator.FindNearestPlacement(window1Handle, window2Handle, offsetX, offsetY);
            }

            var pair = new WindowAttachPair
            {
                Window1Handle = window1Handle,
                Window2Handle = window2Handle,
                Placement = actualPlacement, // Store the actual placement, not Nearest
                OffsetX = offsetX,
                OffsetY = offsetY,
                RestrictToSameScreen = restrictToSameScreen,
                AutoAdjustToScreen = autoAdjustToScreen,
                AttachType = attachType
            };

            var key = pair.GetKey();

            // If already registered, return false
            if (_attachments.ContainsKey(key))
            {
                return false;
            }

            // Create and register new attachment service
            var service = new WindowAttachService();
            service.Attach(window1Handle, window2Handle, actualPlacement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen, attachType, callbackAction);
            
            // Subscribe to window destruction event for auto-unregister
            service.WindowDestroyed += (w1, w2) =>
            {
                // Auto-unregister when either window is destroyed
                Unregister(w1, w2, attachType);
            };
            
            // Subscribe to window2 visibility change event for syncing popup button (only for Main attachments)
            if (attachType == AttachType.Main)
            {
                service.Window2VisibilityChanged += (w2, isVisible) =>
                {
                    SyncPopupVisibility(w2, isVisible);
                };
            }
            
            _attachments[key] = service;

            // Add to cache
            _pairsCache[key] = pair;

            // If this is a main attachment, create popup attachment immediately
            if (attachType == AttachType.Main)
            {
                CreatePopupAttachment(window1Handle, window2Handle, actualPlacement, service);
            }

            return true;
        }

        /// <summary>
        /// Create popup attachment for a main attachment
        /// </summary>
        private void CreatePopupAttachment(IntPtr window1Handle, IntPtr window2Handle, WindowPlacement mainPlacement, WindowAttachService mainService)
        {
            // Calculate popup placement based on main placement
            var popupPlacement = PlacementHelper.GetPopupPlacement(mainPlacement);

            // Create popup window and pass callback action
            var popupWindow = new DetachPopupWindow(window1Handle, window2Handle, mainService.CallbackAction);
            
            // Store popup window reference temporarily (will be updated after SourceInitialized)
            string? popupKey = null;
            
            // Subscribe to main service's WindowDestroyed event to close popup when window2 is destroyed
            mainService.WindowDestroyed += (w1, w2) =>
            {
                // When window2 is destroyed, close the popup window
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        // Find and close popup window for this main attachment
                        if (popupKey != null && _popupWindows.TryGetValue(popupKey, out var popup))
                        {
                            popup.Close();
                        }
                        else
                        {
                            // Fallback: find popup by mapping
                            var keyToRemove = _popupToMainMapping
                                .FirstOrDefault(kvp => kvp.Value.mainWindow1Handle == w1 && kvp.Value.mainWindow2Handle == w2)
                                .Key;
                            if (keyToRemove != null && _popupWindows.TryGetValue(keyToRemove, out var popupWindowToClose))
                            {
                                popupWindowToClose.Close();
                            }
                        }
                    }),
                    System.Windows.Threading.DispatcherPriority.Normal);
            };
            
            // Handle popup window initialization and registration
            // Set up event handler BEFORE showing the window
            popupWindow.SourceInitialized += (s, e) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(popupWindow).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Set WS_EX_NOACTIVATE extended style to prevent popup from getting focus
                    // This MUST be set before window is shown
                    WindowHelper.SetWindowExStyle(hwnd, Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_NOACTIVATE, true);
                    
                    // Set popup owner to window2 to make it follow window2's virtual desktop
                    // Using GWLP_HWNDPARENT instead of SetParent to avoid WPF rendering issues
                    // This ensures popup only shows on the same virtual desktop as window2
                    WindowHelper.SetWindowOwner(hwnd, window2Handle);
                    
                    // Set popup z-order to be the same as window2 (not topmost)
                    // This prevents popup from always showing on top when window1 goes to background
                    WindowHelper.SetWindowZOrder(hwnd, window2Handle, SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                    
                    // Register popup attachment FIRST (before adding to blacklist)
                    // This will position the popup correctly
                    RegisterPopupAttachment(window2Handle, hwnd, popupPlacement);
                    
                    // Add popup handle to blacklist AFTER registration to prevent it from being attached as window1
                    // But allow it to be attached as window2 (which we just did above)
                    _blacklist.Add(hwnd);
                    
                    // Store popup window reference
                    popupKey = GetKey(window2Handle, hwnd, AttachType.Popup);
                    _popupWindows[popupKey] = popupWindow;
                    
                    // Store mapping from popup to main attachment (window1Handle, window2Handle)
                    // This allows popup to know which main attachment it belongs to
                    _popupToMainMapping[popupKey] = (window1Handle, window2Handle);
                    
                    // Force an immediate position update
                    var popupAttachmentKey = GetKey(window2Handle, hwnd, AttachType.Popup);
                    if (_attachments.TryGetValue(popupAttachmentKey, out var popupService))
                    {
                        // Force position update immediately
                        popupService.ForceUpdatePosition();
                    }
                    
                    // Handle popup window closing
                    popupWindow.Closed += (sender, args) =>
                    {
                        // Remove from blacklist when popup is closed
                        _blacklist.Remove(hwnd);
                        if (popupKey != null)
                        {
                            _popupWindows.Remove(popupKey);
                            _popupToMainMapping.Remove(popupKey);
                        }
                    };
                }
            };
            
            // Show popup window AFTER setting up SourceInitialized handler
            // SourceInitialized fires when window handle is created, before window is shown
            popupWindow.Show();
        }

        /// <summary>
        /// Register popup attachment
        /// </summary>
        private void RegisterPopupAttachment(IntPtr window2Handle, IntPtr popupHandle, WindowPlacement popupPlacement)
        {
            // Popup window should always be restricted to screen bounds
            Register(window2Handle, popupHandle, popupPlacement, 0, 0, restrictToSameScreen: true, autoAdjustToScreen: true, AttachType.Popup);
        }

        /// <summary>
        /// Sync popup button visibility based on window2 visibility state
        /// </summary>
        /// <param name="window2Handle">Handle of window2</param>
        /// <param name="isVisible">Whether window2 is visible</param>
        private void SyncPopupVisibility(IntPtr window2Handle, bool isVisible)
        {
            // Find popup attachment for this window2
            var popupPairs = _pairsCache.Values
                .Where(p => p.Window1Handle == window2Handle && p.AttachType == AttachType.Popup)
                .ToList();

            foreach (var popupPair in popupPairs)
            {
                var popupKey = GetKey(popupPair.Window1Handle, popupPair.Window2Handle, AttachType.Popup);
                
                // Get popup service
                if (_attachments.TryGetValue(popupKey, out var popupService))
                {
                    // Force update position which will handle visibility based on window2 state
                    popupService.ForceUpdatePosition();
                }
                
                // Also update popup window visibility directly
                if (_popupWindows.TryGetValue(popupKey, out var popupWindow))
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            if (isVisible)
                            {
                                if (!popupWindow.IsVisible)
                                {
                                    popupWindow.Show();
                                }
                            }
                            else
                            {
                                if (popupWindow.IsVisible)
                                {
                                    popupWindow.Hide();
                                }
                            }
                        }),
                        System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
        }

        /// <summary>
        /// Unregister a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="attachType">Type of attachment (if not specified, will try both Main and Popup)</param>
        /// <returns>True if unregistered successfully, false if not found</returns>
        public bool Unregister(IntPtr window1Handle, IntPtr window2Handle, AttachType? attachType = null)
        {
            // If attachType is specified, try that type first
            if (attachType.HasValue)
            {
                var key = GetKey(window1Handle, window2Handle, attachType.Value);
                if (UnregisterByKey(key))
                {
                    // If this is a main attachment, also unregister popup
                    if (attachType.Value == AttachType.Main)
                    {
                        UnregisterPopupForMain(window1Handle, window2Handle);
                    }
                    return true;
                }
                return false;
            }

            // Try Main attachment first
            var mainKey = GetKey(window1Handle, window2Handle, AttachType.Main);
            if (UnregisterByKey(mainKey))
            {
                // Also unregister popup for this main attachment
                UnregisterPopupForMain(window1Handle, window2Handle);
                return true;
            }

            // Try Popup attachment
            var popupKey = GetKey(window1Handle, window2Handle, AttachType.Popup);
            return UnregisterByKey(popupKey);
        }

        /// <summary>
        /// Unregister attachment by key
        /// </summary>
        private bool UnregisterByKey(string key)
        {
            if (!_attachments.TryGetValue(key, out var service))
            {
                return false;
            }

            service.Dispose();
            _attachments.Remove(key);

            // Remove from cache
            _pairsCache.Remove(key);

            return true;
        }

        /// <summary>
        /// Unregister popup attachment for a main attachment
        /// </summary>
        private void UnregisterPopupForMain(IntPtr window1Handle, IntPtr window2Handle)
        {
            // Find popup that belongs to this specific main attachment (window1Handle, window2Handle)
            // Use the mapping to find the correct popup
            var popupKeyToRemove = _popupToMainMapping
                .FirstOrDefault(kvp => kvp.Value.mainWindow1Handle == window1Handle && kvp.Value.mainWindow2Handle == window2Handle)
                .Key;

            if (popupKeyToRemove != null)
            {
                // Get popup pair from cache
                if (_pairsCache.TryGetValue(popupKeyToRemove, out var popupPair))
                {
                    
                    // Remove popup handle from blacklist
                    _blacklist.Remove(popupPair.Window2Handle);
                    
                    // Close popup window if exists
                    if (_popupWindows.TryGetValue(popupKeyToRemove, out var popupWindow))
                    {
                        popupWindow.Close();
                        _popupWindows.Remove(popupKeyToRemove);
                    }
                    
                    // Remove from mapping
                    _popupToMainMapping.Remove(popupKeyToRemove);

                    // Unregister popup attachment
                    UnregisterByKey(popupPair.GetKey());
                }
            }
        }

        /// <summary>
        /// Check if a window attachment is registered
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="attachType">Type of attachment (if not specified, checks Main type)</param>
        /// <returns>True if registered</returns>
        public bool IsRegistered(IntPtr window1Handle, IntPtr window2Handle, AttachType attachType = AttachType.Main)
        {
            var key = GetKey(window1Handle, window2Handle, attachType);
            return _attachments.ContainsKey(key);
        }

        /// <summary>
        /// Toggle window attachment (register if not registered, unregister if registered)
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="placement">Placement position (used when registering)</param>
        /// <param name="offsetX">Horizontal offset (used when registering)</param>
        /// <param name="offsetY">Vertical offset (used when registering)</param>
        /// <param name="restrictToSameScreen">Whether to restrict window2 to the same screen as window1 (used when registering)</param>
        /// <param name="autoAdjustToScreen">Whether to automatically adjust position to maximize visible area when window is not fully visible (used when registering)</param>
        /// <param name="callbackAction">Callback action to execute when window1 is closed (default: null)</param>
        /// <returns>True if registered, false if unregistered</returns>
        public bool Toggle(IntPtr window1Handle, IntPtr window2Handle, WindowPlacement placement = WindowPlacement.RightTop,
            double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false, Action? callbackAction = null)
        {
            if (IsRegistered(window1Handle, window2Handle, AttachType.Main))
            {
                Unregister(window1Handle, window2Handle);
                return false;
            }
            else
            {
                Register(window1Handle, window2Handle, placement, offsetX, offsetY, restrictToSameScreen, autoAdjustToScreen, AttachType.Main, callbackAction);
                return true;
            }
        }

        /// <summary>
        /// Unregister all attachments
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var service in _attachments.Values)
            {
                service.Dispose();
            }
            _attachments.Clear();
            _pairsCache.Clear();
            _popupWindows.Clear(); // Clear popup windows
            _popupToMainMapping.Clear(); // Clear popup to main mapping
            _blacklist.Clear(); // Clear blacklist
        }

        /// <summary>
        /// Get all registered attachment keys
        /// </summary>
        /// <returns>List of attachment keys</returns>
        public IEnumerable<string> GetRegisteredKeys()
        {
            return _attachments.Keys.ToList();
        }

        /// <summary>
        /// Get all registered window pairs
        /// </summary>
        /// <returns>List of window pairs (window1Handle, window2Handle)</returns>
        public IEnumerable<(IntPtr window1Handle, IntPtr window2Handle)> GetRegisteredPairs()
        {
            return _pairsCache.Values.Select(pair => (pair.Window1Handle, pair.Window2Handle));
        }

        /// <summary>
        /// Update placement for an existing attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="newPlacement">New placement position</param>
        /// <returns>True if updated successfully, false if not found</returns>
        public bool UpdatePlacement(IntPtr window1Handle, IntPtr window2Handle, WindowPlacement newPlacement)
        {
            var key = GetKey(window1Handle, window2Handle, AttachType.Main);
            
            if (!_attachments.TryGetValue(key, out var service))
            {
                return false;
            }
            
            // Get current pair from cache
            if (!_pairsCache.TryGetValue(key, out var pair))
            {
                return false;
            }
            
            // Update the service settings
            service.UpdateSettings(newPlacement, pair.OffsetX, pair.OffsetY, pair.RestrictToSameScreen, pair.AutoAdjustToScreen);
            
            // Update the pair in cache
            pair.Placement = newPlacement;
            _pairsCache[key] = pair;
            
            // If this is a main attachment, update popup placement as well
            var popupPlacement = PlacementHelper.GetPopupPlacement(newPlacement);
            UpdatePopupPlacement(window2Handle, popupPlacement);
            
            return true;
        }
        
        /// <summary>
        /// Get a window attachment pair by key
        /// </summary>
        /// <param name="key">Attachment key</param>
        /// <returns>Window attachment pair if found, null otherwise</returns>
        public WindowAttachPair? GetPair(string key)
        {
            return _pairsCache.TryGetValue(key, out var pair) ? pair : null;
        }
        
        /// <summary>
        /// Update settings for an existing attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="restrictToSameScreen">Whether to restrict to same screen</param>
        /// <param name="autoAdjustToScreen">Whether to auto-adjust to screen</param>
        /// <returns>True if updated successfully, false if not found</returns>
        public bool UpdateSettings(IntPtr window1Handle, IntPtr window2Handle, bool restrictToSameScreen, bool autoAdjustToScreen)
        {
            var key = GetKey(window1Handle, window2Handle, AttachType.Main);
            
            if (!_attachments.TryGetValue(key, out var service))
            {
                return false;
            }
            
            // Get current pair from cache
            if (!_pairsCache.TryGetValue(key, out var pair))
            {
                return false;
            }
            
            // Update the service settings
            service.UpdateSettings(pair.Placement, pair.OffsetX, pair.OffsetY, restrictToSameScreen, autoAdjustToScreen);
            
            // Update the pair in cache
            pair.RestrictToSameScreen = restrictToSameScreen;
            pair.AutoAdjustToScreen = autoAdjustToScreen;
            _pairsCache[key] = pair;
            
            return true;
        }
        
        /// <summary>
        /// Update popup placement for a main attachment
        /// </summary>
        private void UpdatePopupPlacement(IntPtr window2Handle, WindowPlacement popupPlacement)
        {
            // Find popup attachment for this window2
            var popupPairs = _pairsCache.Values
                .Where(p => p.Window1Handle == window2Handle && p.AttachType == AttachType.Popup)
                .ToList();
            
            foreach (var popupPair in popupPairs)
            {
                var popupKey = GetKey(popupPair.Window1Handle, popupPair.Window2Handle, AttachType.Popup);
                
                if (_attachments.TryGetValue(popupKey, out var popupService))
                {
                    // Update popup service settings
                    popupService.UpdateSettings(popupPlacement, popupPair.OffsetX, popupPair.OffsetY, 
                        popupPair.RestrictToSameScreen, popupPair.AutoAdjustToScreen);
                    
                    // Update the pair in cache
                    popupPair.Placement = popupPlacement;
                    _pairsCache[popupKey] = popupPair;
                }
            }
        }

        /// <summary>
        /// Generate a unique key for a window pair
        /// </summary>
        private static string GetKey(IntPtr window1Handle, IntPtr window2Handle, AttachType attachType = AttachType.Main)
        {
            // Use both handles and attach type to create a unique key
            // Order matters: window1_window2_attachType
            return $"{window1Handle}_{window2Handle}_{attachType}";
        }

        public void Dispose()
        {
            UnregisterAll();
        }
    }
}
