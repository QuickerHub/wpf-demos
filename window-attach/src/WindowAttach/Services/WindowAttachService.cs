using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowAttach.Models;
using WindowAttach.Utils;
using log4net;

namespace WindowAttach.Services
{
    /// <summary>
    /// Service for attaching window2 to window1, making window2 follow window1 permanently
    /// </summary>
    public class WindowAttachService : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WindowAttachService));
        
        private HWND _window1Handle;
        private HWND _window2Handle;
        private WindowPlacement _placement;
        private double _offsetX;
        private double _offsetY;
        private bool _restrictToSameScreen;
        private bool _autoAdjustToScreen;
        private bool _isAttached;
        private WindowEventHook? _window1EventHook;
        private WindowEventHook? _window2EventHook;
        private NoActivateWindowHook? _noActivateHook;
        private bool _isUpdatingPosition;
        private AttachType _attachType = AttachType.Main;
        private bool? _originalToolWindowState; // Track original WS_EX_TOOLWINDOW state
        private Action? _callbackAction; // Callback action to execute when window1 is closed

        /// <summary>
        /// Get the callback action (for popup window to access)
        /// </summary>
        public Action? CallbackAction => _callbackAction;

        /// <summary>
        /// Event raised when either window is destroyed
        /// </summary>
        public event Action<IntPtr, IntPtr>? WindowDestroyed;

        /// <summary>
        /// Event raised when window2 visibility state changes (for syncing popup button)
        /// </summary>
        public event Action<IntPtr, bool>? Window2VisibilityChanged;

        /// <summary>
        /// Initialize window attachment
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
        public void Attach(IntPtr window1Handle, IntPtr window2Handle, WindowPlacement placement, 
            double offsetX = 0, double offsetY = 0, bool restrictToSameScreen = false, bool autoAdjustToScreen = false, AttachType attachType = AttachType.Main, Action? callbackAction = null)
        {
            var hwnd1 = new HWND(window1Handle);
            var hwnd2 = new HWND(window2Handle);

            if (!IsWindow(hwnd1) || !IsWindow(hwnd2))
                throw new ArgumentException("Invalid window handles");

            Detach();

            _window1Handle = hwnd1;
            _window2Handle = hwnd2;
            _placement = placement;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _restrictToSameScreen = restrictToSameScreen;
            _autoAdjustToScreen = autoAdjustToScreen;
            _attachType = attachType;
            _callbackAction = callbackAction;
            _isAttached = true;

            // Set window2 owner to window1 to make it follow window1's virtual desktop
            // This ensures window2 only shows on the same virtual desktop as window1
            // Only apply to Main attachments (popup attachments already handle this separately)
            if (attachType == AttachType.Main)
            {
                // Record original tool window state before modifying
                _originalToolWindowState = WindowHelper.IsToolWindow(window2Handle);
                
                // Set window2 to not show in taskbar (WS_EX_TOOLWINDOW)
                // This makes window2 behave like a tool window that follows window1
                if (!_originalToolWindowState.Value)
                {
                    WindowHelper.SetWindowExStyle(window2Handle, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, true);
                }
                
                WindowHelper.SetWindowOwner(window2Handle, window1Handle);
                
                // Install window hook to prevent window2 from getting focus when clicked
                // Only install hook if window2 is already a no-activate window
                if (WindowHelper.IsNoActivateWindow(window2Handle))
                {
                    try
                    {
                        _noActivateHook = new NoActivateWindowHook(window2Handle);
                    }
                    catch
                    {
                        // If hook installation fails, continue without it
                        _noActivateHook = null;
                    }
                }
            }

            // Start unified event hooks for both windows
            StartEventHooks();

            // Initial position update
            UpdatePosition();
        }

        /// <summary>
        /// Detach windows
        /// </summary>
        public void Detach()
        {
            if (_isAttached)
            {
                // Clear window2 owner to restore its independence
                // This removes the virtual desktop following relationship
                // Only apply to Main attachments (popup attachments handle this separately)
                if (_attachType == AttachType.Main && _window2Handle.Value != IntPtr.Zero)
                {
                    // Check if window2 still exists before operating on it
                    bool window2Exists = IsWindow(_window2Handle);
                    
                    if (window2Exists)
                    {
                        // Dispose the no-activate hook first
                        _noActivateHook?.Dispose();
                        _noActivateHook = null;
                        
                        // Restore original tool window state
                        if (_originalToolWindowState.HasValue)
                        {
                            WindowHelper.SetWindowExStyle(_window2Handle.Value, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, _originalToolWindowState.Value);
                        }
                        
                        // Clear window2 owner to restore its independence
                        WindowHelper.SetWindowOwner(_window2Handle.Value, IntPtr.Zero);
                        
                        // When window1 is destroyed, Windows may automatically hide window2 (because it was owned by window1)
                        // We need to explicitly show window2 if it was hidden due to owner destruction
                        // Check if window2 is hidden (not visible) but not minimized
                        bool isWindow2Hidden = !IsWindowVisible(_window2Handle);
                        bool isWindow2Minimized = IsIconic(_window2Handle);
                        
                        if (isWindow2Hidden && !isWindow2Minimized)
                        {
                            // Window2 is hidden but not minimized - likely hidden due to owner destruction
                            // Restore it to visible state
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                        }
                        else if (isWindow2Minimized)
                        {
                            // Window2 is minimized - restore it
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                        }
                    }
                }
            }

            _isAttached = false;
            StopEventHooks();
            
            // Release callback action
            _callbackAction = null;
            
            // Reset original tool window state tracking
            _originalToolWindowState = null;
            
            _window1Handle = HWND.Null;
            _window2Handle = HWND.Null;
        }

        /// <summary>
        /// Update placement settings
        /// </summary>
        public void UpdateSettings(WindowPlacement placement, double offsetX, double offsetY, bool restrictToSameScreen, bool autoAdjustToScreen)
        {
            _placement = placement;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _restrictToSameScreen = restrictToSameScreen;
            _autoAdjustToScreen = autoAdjustToScreen;
            
            if (_isAttached)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Force an immediate position update
        /// </summary>
        public void ForceUpdatePosition()
        {
            if (_isAttached)
            {
                UpdatePosition();
            }
        }

        private void StartEventHooks()
        {
            StopEventHooks();

            // Create unified hook for window1 (monitors location changes, visibility, activation, and destruction)
            _window1EventHook = new WindowEventHook(_window1Handle.Value);
            _window1EventHook.LocationChanged += OnWindow1LocationChanged;
            _window1EventHook.VisibilityChanged += OnWindow1VisibilityChanged;
            _window1EventHook.Activated += OnWindow1Activated;
            _window1EventHook.Destroyed += OnWindow1Destroyed;
            _window1EventHook.StartHook();

            // Create unified hook for window2 (monitors visibility and destruction)
            _window2EventHook = new WindowEventHook(_window2Handle.Value);
            _window2EventHook.VisibilityChanged += OnWindow2VisibilityChanged;
            _window2EventHook.Destroyed += OnWindow2Destroyed;
            _window2EventHook.StartHook();
        }

        private void StopEventHooks()
        {
            if (_window1EventHook != null)
            {
                _window1EventHook.LocationChanged -= OnWindow1LocationChanged;
                _window1EventHook.VisibilityChanged -= OnWindow1VisibilityChanged;
                _window1EventHook.Activated -= OnWindow1Activated;
                _window1EventHook.Destroyed -= OnWindow1Destroyed;
                _window1EventHook.Dispose();
                _window1EventHook = null;
            }

            if (_window2EventHook != null)
            {
                _window2EventHook.VisibilityChanged -= OnWindow2VisibilityChanged;
                _window2EventHook.Destroyed -= OnWindow2Destroyed;
                _window2EventHook.Dispose();
                _window2EventHook = null;
            }
        }

        private void OnWindow1LocationChanged(IntPtr hwnd)
        {
            // Use Dispatcher to ensure UpdatePosition runs on UI thread
            Application.Current.Dispatcher.BeginInvoke(
                new Action(UpdatePosition),
                DispatcherPriority.Normal);
        }

        private void OnWindow1VisibilityChanged(IntPtr hwnd, bool isVisible)
        {
            // Sync window2 visibility with window1
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => SyncWindow2Visibility(isVisible)),
                DispatcherPriority.Normal);
            
            // Also update position (which handles some edge cases)
            Application.Current.Dispatcher.BeginInvoke(
                new Action(UpdatePosition),
                DispatcherPriority.Normal);
        }

        /// <summary>
        /// Sync window2 visibility with window1 visibility state
        /// </summary>
        private void SyncWindow2Visibility(bool window1IsVisible)
        {
            if (!_isAttached)
                return;

            // Check if windows still exist
            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
                return;

            // Check if window2 is a tool window (not shown in taskbar) or a popup attachment
            bool isWindow2ToolWindow = WindowHelper.IsToolWindow(_window2Handle.Value);
            bool isPopupAttachment = _attachType == AttachType.Popup;

            bool window2WasVisible = IsWindowVisible(_window2Handle) && !IsIconic(_window2Handle);
            bool window2VisibilityChanged = false;
            bool window2WillBeVisible = window2WasVisible;

            if (!window1IsVisible)
            {
                // Window1 is hidden, hide window2
                if (isPopupAttachment || isWindow2ToolWindow)
                {
                    // Hide window2 if it's a popup attachment or tool window (not shown in taskbar)
                    if (IsWindowVisible(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_HIDE);
                        window2WillBeVisible = false;
                        window2VisibilityChanged = true;
                    }
                }
                else
                {
                    // Minimize window2 if window1 is hidden (normal window)
                    if (!IsIconic(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                        window2WillBeVisible = false;
                        window2VisibilityChanged = true;
                    }
                }
            }
            else
            {
                // Window1 is visible, show window2
                if (isPopupAttachment || isWindow2ToolWindow)
                {
                    // Show window2 without activating if it's a popup attachment or tool window
                    if (!IsWindowVisible(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                        window2WillBeVisible = true;
                        window2VisibilityChanged = true;
                    }
                }
                else
                {
                    // Restore window2 if window1 is restored (normal window)
                    if (IsIconic(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                        window2WillBeVisible = true;
                        window2VisibilityChanged = true;
                    }
                }
            }

            // Notify visibility change for syncing popup button (only for Main attachments)
            // This ensures popup is hidden/shown immediately when window2 visibility changes
            if (_attachType == AttachType.Main && window2VisibilityChanged)
            {
                Window2VisibilityChanged?.Invoke(_window2Handle.Value, window2WillBeVisible);
            }
        }

        private void OnWindow1Activated(IntPtr hwnd)
        {
            // When window1 is activated, adjust window2's z-order to be visible but not activated
            Application.Current.Dispatcher.BeginInvoke(
                new Action(AdjustWindow2ZOrder),
                DispatcherPriority.Normal);
        }

        /// <summary>
        /// Adjust window2's z-order to be visible when window1 is activated, but don't activate window2
        /// </summary>
        private void AdjustWindow2ZOrder()
        {
            if (!_isAttached)
                return;

            // Check if windows still exist
            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
                return;

            // Check if window1 is visible and not minimized
            if (!IsWindowVisible(_window1Handle) || IsIconic(_window1Handle))
                return;

            // Check if window2 is visible and not minimized
            if (!IsWindowVisible(_window2Handle) || IsIconic(_window2Handle))
                return;

            // Set window2's z-order to be right after window1, but don't activate it
            // This makes window2 visible when window1 is in foreground, but window2 won't steal focus
            WindowHelper.SetWindowZOrder(_window2Handle.Value, _window1Handle.Value, 
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }

        private void OnWindow2VisibilityChanged(IntPtr hwnd, bool isVisible)
        {
            // Notify visibility change for syncing popup button (only for Main attachments)
            if (_attachType == AttachType.Main)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => Window2VisibilityChanged?.Invoke(hwnd, isVisible)),
                    DispatcherPriority.Normal);
            }
        }

        private void OnWindow1Destroyed(IntPtr hwnd)
        {
            // Use Dispatcher to ensure callback runs on UI thread
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    // Execute callback action if provided (before detaching)
                    if (_callbackAction != null)
                    {
                        try
                        {
                            _callbackAction.Invoke();
                        }
                        catch (Exception ex)
                        {
                            // Log error when callback action fails
                            _log.Error($"Failed to execute callback action when window1 (handle: {_window1Handle.Value}) was destroyed", ex);
                        }
                    }
                    
                    // Notify that window1 was destroyed
                    WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
                    Detach();
                }),
                DispatcherPriority.Normal);
        }

        private void OnWindow2Destroyed(IntPtr hwnd)
        {
            // Use Dispatcher to ensure callback runs on UI thread
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    // Notify that window2 was destroyed
                    WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
                    Detach();
                }),
                DispatcherPriority.Normal);
        }

        private void UpdatePosition()
        {
            if (!_isAttached || _isUpdatingPosition)
                return;

            // Check if windows still exist, if not, auto-detach
            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
            {
                // Windows were destroyed, trigger event and detach
                WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
                Detach();
                return;
            }

            _isUpdatingPosition = true;

            try
            {
                // Check if window2 is a tool window (not shown in taskbar) or a popup attachment
                bool isWindow2ToolWindow = WindowHelper.IsToolWindow(_window2Handle.Value);
                bool isPopupAttachment = _attachType == AttachType.Popup;

                // Check if window1 is minimized or hidden (both count as hidden)
                bool isWindow1Hidden = IsIconic(_window1Handle) || !IsWindowVisible(_window1Handle);

                bool window2WasVisible = IsWindowVisible(_window2Handle) && !IsIconic(_window2Handle);
                bool window2VisibilityChanged = false;
                bool window2WillBeVisible = window2WasVisible;

                if (isWindow1Hidden)
                {
                    if (isPopupAttachment || isWindow2ToolWindow)
                    {
                        // Hide window2 if it's a popup attachment or tool window (not shown in taskbar)
                        if (IsWindowVisible(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_HIDE);
                            window2WillBeVisible = false;
                            window2VisibilityChanged = true;
                        }
                    }
                    else
                    {
                        // Minimize window2 if window1 is hidden (normal window)
                        if (!IsIconic(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                            window2WillBeVisible = false;
                            window2VisibilityChanged = true;
                        }
                    }
                    
                    // Notify visibility change for syncing popup button (only for Main attachments)
                    if (_attachType == AttachType.Main && window2VisibilityChanged)
                    {
                        Window2VisibilityChanged?.Invoke(_window2Handle.Value, window2WillBeVisible);
                    }
                    
                    return;
                }
                else
                {
                    if (isPopupAttachment || isWindow2ToolWindow)
                    {
                        // Show window2 without activating if it's a popup attachment or tool window
                        if (!IsWindowVisible(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                            window2WillBeVisible = true;
                            window2VisibilityChanged = true;
                        }
                    }
                    else
                    {
                        // Restore window2 if window1 is restored (normal window)
                        if (IsIconic(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                            window2WillBeVisible = true;
                            window2VisibilityChanged = true;
                        }
                    }
                    
                    // Notify visibility change for syncing popup button (only for Main attachments)
                    if (_attachType == AttachType.Main && window2VisibilityChanged)
                    {
                        Window2VisibilityChanged?.Invoke(_window2Handle.Value, window2WillBeVisible);
                    }
                }

                // Get window1 rectangle (physical pixels)
                var window1Rect = WindowHelper.GetWindowRect(_window1Handle.Value);
                if (window1Rect == null)
                    return;

                // Get window2 rectangle (physical pixels)
                var window2Rect = WindowHelper.GetWindowRect(_window2Handle.Value);
                if (window2Rect == null)
                    return;

                // Calculate window dimensions in physical pixels
                int window1Left = window1Rect.Value.Left;
                int window1Top = window1Rect.Value.Top;
                int window1Width = window1Rect.Value.Width;
                int window1Height = window1Rect.Value.Height;
                int window2Width = window2Rect.Value.Width;
                int window2Height = window2Rect.Value.Height;

                // Calculate window2 position based on placement (all in physical pixels)
                int window2X = 0, window2Y = 0;
                int offsetX = (int)_offsetX;
                int offsetY = (int)_offsetY;

                switch (_placement)
                {
                    case WindowPlacement.LeftTop:
                        window2X = window1Left - window2Width - offsetX;
                        window2Y = window1Top + offsetY;
                        break;
                    case WindowPlacement.LeftCenter:
                        window2X = window1Left - window2Width - offsetX;
                        window2Y = window1Top + (window1Height - window2Height) / 2 + offsetY;
                        break;
                    case WindowPlacement.LeftBottom:
                        window2X = window1Left - window2Width - offsetX;
                        window2Y = window1Top + window1Height - window2Height - offsetY;
                        break;
                    case WindowPlacement.TopLeft:
                        window2X = window1Left + offsetX;
                        window2Y = window1Top - window2Height - offsetY;
                        break;
                    case WindowPlacement.TopCenter:
                        window2X = window1Left + (window1Width - window2Width) / 2 + offsetX;
                        window2Y = window1Top - window2Height - offsetY;
                        break;
                    case WindowPlacement.TopRight:
                        window2X = window1Left + window1Width - window2Width - offsetX;
                        window2Y = window1Top - window2Height - offsetY;
                        break;
                    case WindowPlacement.RightTop:
                        window2X = window1Left + window1Width + offsetX;
                        window2Y = window1Top + offsetY;
                        break;
                    case WindowPlacement.RightCenter:
                        window2X = window1Left + window1Width + offsetX;
                        window2Y = window1Top + (window1Height - window2Height) / 2 + offsetY;
                        break;
                    case WindowPlacement.RightBottom:
                        window2X = window1Left + window1Width + offsetX;
                        window2Y = window1Top + window1Height - window2Height - offsetY;
                        break;
                    case WindowPlacement.BottomLeft:
                        window2X = window1Left + offsetX;
                        window2Y = window1Top + window1Height + offsetY;
                        break;
                    case WindowPlacement.BottomCenter:
                        window2X = window1Left + (window1Width - window2Width) / 2 + offsetX;
                        window2Y = window1Top + window1Height + offsetY;
                        break;
                    case WindowPlacement.BottomRight:
                        window2X = window1Left + window1Width - window2Width - offsetX;
                        window2Y = window1Top + window1Height + offsetY;
                        break;
                    case WindowPlacement.Nearest:
                        // Nearest should have been converted to actual placement during registration
                        // If we reach here, default to RightTop
                        window2X = window1Left + window1Width + offsetX;
                        window2Y = window1Top + offsetY;
                        break;
                }

                // Auto-adjust position to maximize visible area if enabled (excluding overlap with window1)
                // Tries all placement positions and selects the one with maximum visible area
                // Prioritizes opposite placement (e.g., LeftTop -> RightTop)
                if (_autoAdjustToScreen)
                {
                    var (adjustedX, adjustedY) = ScreenAdjustHelper.AdjustPositionToScreen(
                        window2X, window2Y, window2Width, window2Height, _window2Handle.Value, window1Rect.Value, _placement, _offsetX, _offsetY);
                    window2X = adjustedX;
                    window2Y = adjustedY;
                }

                // Get screen bounds if restriction is enabled (applied after autoAdjustToScreen if enabled)
                // This ensures restrictToSameScreen works on the adjusted position
                if (_restrictToSameScreen)
                {
                    var workArea = WindowHelper.GetMonitorWorkArea(_window1Handle.Value);
                    if (workArea != null)
                    {
                        int screenLeft = workArea.Value.Left;
                        int screenTop = workArea.Value.Top;
                        int screenRight = workArea.Value.Right;
                        int screenBottom = workArea.Value.Bottom;

                        // Constrain window2 position to screen bounds
                        if (window2X < screenLeft)
                            window2X = screenLeft;
                        if (window2Y < screenTop)
                            window2Y = screenTop;
                        if (window2X + window2Width > screenRight)
                            window2X = screenRight - window2Width;
                        if (window2Y + window2Height > screenBottom)
                            window2Y = screenBottom - window2Height;
                    }
                }

                // Set window position (all values are already in physical pixels)
                if (_attachType == AttachType.Popup)
                {
                    // For popup attachments, set z-order to be the same as window1 (which is window2 in the main attachment)
                    // This ensures popup follows window2's z-order and doesn't stay on top when window1 goes to background
                    WindowHelper.SetWindowZOrder(_window2Handle.Value, _window1Handle.Value, 
                        SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                    // Then set position (with SWP_NOZORDER to preserve the z-order we just set)
                    WindowHelper.SetWindowPos(_window2Handle.Value, window2X, window2Y, window2Width, window2Height,
                        SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                }
                else
                {
                    // For main attachments, use normal position update
                    WindowHelper.SetWindowPos(_window2Handle.Value, window2X, window2Y, window2Width, window2Height,
                        SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                }
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        public void Dispose()
        {
            // Dispose no-activate hook
            _noActivateHook?.Dispose();
            _noActivateHook = null;
            
            Detach();
        }
    }
}

