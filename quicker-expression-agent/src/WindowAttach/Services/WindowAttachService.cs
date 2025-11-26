using System;
using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowAttach.Models;
using WindowAttach.Utils;
using WINDOW_EX_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using SET_WINDOW_POS_FLAGS = Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS;

namespace WindowAttach.Services
{
    /// <summary>
    /// Service for managing window attachments
    /// </summary>
    public class WindowAttachService : IDisposable
    {
        private readonly Dictionary<string, WindowAttachment> _attachments = new Dictionary<string, WindowAttachment>();

        /// <summary>
        /// Register a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window (window to follow)</param>
        /// <param name="window2Handle">Handle of the window to attach (window that follows)</param>
        /// <param name="attachParams">Attachment parameters</param>
        /// <returns>True if registered successfully, false if already registered</returns>
        public bool Register(IntPtr window1Handle, IntPtr window2Handle, AttachParams attachParams)
        {
            if (window1Handle == window2Handle)
            {
                throw new ArgumentException("Window1 and Window2 cannot be the same window");
            }

            var key = GetKey(window1Handle, window2Handle);

            // If already registered, return false
            if (_attachments.ContainsKey(key))
            {
                return false;
            }

            // Create and register new attachment
            var attachment = new WindowAttachment(window1Handle, window2Handle, attachParams);
            attachment.WindowDestroyed += (w1, w2) =>
            {
                // Auto-unregister when either window is destroyed
                Unregister(w1, w2);
            };

            _attachments[key] = attachment;
            return true;
        }

        /// <summary>
        /// Unregister a window attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <returns>True if unregistered successfully, false if not found</returns>
        public bool Unregister(IntPtr window1Handle, IntPtr window2Handle)
        {
            var key = GetKey(window1Handle, window2Handle);

            if (!_attachments.TryGetValue(key, out var attachment))
            {
                return false;
            }

            attachment.Dispose();
            _attachments.Remove(key);
            return true;
        }

        /// <summary>
        /// Update attachment parameters for an existing attachment
        /// </summary>
        /// <param name="window1Handle">Handle of the target window</param>
        /// <param name="window2Handle">Handle of the attached window</param>
        /// <param name="attachParams">New attachment parameters</param>
        /// <returns>True if updated successfully, false if not found</returns>
        public bool Update(IntPtr window1Handle, IntPtr window2Handle, AttachParams attachParams)
        {
            var key = GetKey(window1Handle, window2Handle);

            if (!_attachments.TryGetValue(key, out var attachment))
            {
                return false;
            }

            attachment.UpdateParams(attachParams);
            return true;
        }

        /// <summary>
        /// Check if a window attachment is registered
        /// </summary>
        public bool IsRegistered(IntPtr window1Handle, IntPtr window2Handle)
        {
            var key = GetKey(window1Handle, window2Handle);
            return _attachments.ContainsKey(key);
        }

        /// <summary>
        /// Unregister all attachments
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var attachment in _attachments.Values)
            {
                attachment.Dispose();
            }
            _attachments.Clear();
        }

        /// <summary>
        /// Generate a unique key for a window pair
        /// </summary>
        private static string GetKey(IntPtr window1Handle, IntPtr window2Handle)
        {
            return $"{window1Handle}_{window2Handle}";
        }

        public void Dispose()
        {
            UnregisterAll();
        }
    }

    /// <summary>
    /// Internal class representing a window attachment
    /// </summary>
    internal class WindowAttachment : IDisposable
    {
        private HWND _window1Handle;
        private HWND _window2Handle;
        private WindowPlacement _placement;
        private double _offsetX;
        private double _offsetY;
        private bool _restrictToSameScreen;
        private bool _autoOptimizePosition;
        private bool _isAttached;
        private WindowEventHook? _window1EventHook;
        private WindowEventHook? _window2EventHook;
        private bool _isUpdatingPosition;
        private bool? _originalToolWindowState;
        private Action? _callbackAction;
        private bool _preventActivation;

        /// <summary>
        /// Event raised when either window is destroyed
        /// </summary>
        public event Action<IntPtr, IntPtr>? WindowDestroyed;

        public WindowAttachment(IntPtr window1Handle, IntPtr window2Handle, AttachParams attachParams)
        {
            _window1Handle = new HWND(window1Handle);
            _window2Handle = new HWND(window2Handle);

            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
                throw new ArgumentException("Invalid window handles");

            _callbackAction = attachParams.CallbackAction;
            _preventActivation = attachParams.PreventActivation;
            _isAttached = true;

            // Set window2 owner to window1
            _originalToolWindowState = WindowHelper.IsToolWindow(window2Handle);
            
            if (!_originalToolWindowState.Value)
            {
                WindowHelper.SetWindowExStyle(window2Handle, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, true);
            }
            
            // Set window2 owner to window1 using Win32 API
            // This sets GWLP_HWNDPARENT which makes window2 follow window1's virtual desktop
            // and ensures proper window hierarchy
            WindowHelper.SetWindowOwner(window2Handle, window1Handle, preventActivation: _preventActivation);

            // Start event hooks
            StartEventHooks();

            // Update parameters and initial position
            UpdateParams(attachParams);
        }

        public void UpdateParams(AttachParams attachParams)
        {
            _placement = attachParams.Placement;
            _offsetX = attachParams.OffsetX;
            _offsetY = attachParams.OffsetY;
            _restrictToSameScreen = attachParams.RestrictToSameScreen;
            _autoOptimizePosition = attachParams.AutoOptimizePosition;
            _callbackAction = attachParams.CallbackAction;
            
            // Update PreventActivation if changed
            if (_preventActivation != attachParams.PreventActivation)
            {
                _preventActivation = attachParams.PreventActivation;
                WindowHelper.SetWindowOwner(_window2Handle.Value, _window1Handle.Value, preventActivation: _preventActivation);
            }
            
            if (_isAttached)
            {
                UpdatePosition();
            }
        }

        private void StartEventHooks()
        {
            StopEventHooks();

            _window1EventHook = new WindowEventHook(_window1Handle.Value);
            _window1EventHook.LocationChanged += OnWindow1LocationChanged;
            _window1EventHook.VisibilityChanged += OnWindow1VisibilityChanged;
            _window1EventHook.Activated += OnWindow1Activated;
            _window1EventHook.Destroyed += OnWindow1Destroyed;
            _window1EventHook.StartHook();

            _window2EventHook = new WindowEventHook(_window2Handle.Value);
            _window2EventHook.Destroyed += OnWindow2Destroyed;
            _window2EventHook.StartHook();
        }

        private void StopEventHooks()
        {
            _window1EventHook?.Dispose();
            _window1EventHook = null;

            _window2EventHook?.Dispose();
            _window2EventHook = null;
        }

        private void OnWindow1LocationChanged(IntPtr hwnd)
        {
            UpdatePosition();
        }

        private void OnWindow1VisibilityChanged(IntPtr hwnd, bool isVisible)
        {
            SyncWindow2Visibility(isVisible);
            UpdatePosition();
        }

        private void SyncWindow2Visibility(bool window1IsVisible)
        {
            if (!_isAttached)
                return;

            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
                return;

            bool isWindow2ToolWindow = WindowHelper.IsToolWindow(_window2Handle.Value);

            if (!window1IsVisible)
            {
                if (isWindow2ToolWindow)
                {
                    if (IsWindowVisible(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_HIDE);
                    }
                }
                else
                {
                    if (!IsIconic(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                    }
                }
            }
            else
            {
                if (isWindow2ToolWindow)
                {
                    if (!IsWindowVisible(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                    }
                }
                else
                {
                    if (IsIconic(_window2Handle))
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                    }
                }
            }
        }

        private void OnWindow1Activated(IntPtr hwnd)
        {
            AdjustWindow2ZOrder();
        }

        private void AdjustWindow2ZOrder()
        {
            if (!_isAttached)
                return;

            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
                return;

            if (!IsWindowVisible(_window1Handle) || IsIconic(_window1Handle))
                return;

            if (!IsWindowVisible(_window2Handle) || IsIconic(_window2Handle))
                return;

            WindowHelper.SetWindowZOrder(_window2Handle.Value, _window1Handle.Value, 
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);
        }

        private void OnWindow1Destroyed(IntPtr hwnd)
        {
            IntPtr window2Handle = _window2Handle.Value;
            bool isWindow2ToolWindow = _isAttached && IsWindow(_window2Handle) 
                ? WindowHelper.IsToolWindow(_window2Handle.Value) 
                : false;

            if (_callbackAction != null)
            {
                try
                {
                    _callbackAction.Invoke();
                }
                catch
                {
                    // Ignore callback errors
                }
            }
            
            WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
            Detach();

            // Delay restore window2 if needed
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
            {
                try
                {
                    if (window2Handle != IntPtr.Zero && IsWindow(new HWND(window2Handle)))
                    {
                        var hwnd2 = new HWND(window2Handle);
                        if (!IsWindowVisible(hwnd2) || IsIconic(hwnd2))
                        {
                            if (isWindow2ToolWindow)
                            {
                                ShowWindow(hwnd2, SHOW_WINDOW_CMD.SW_SHOWNA);
                            }
                            else
                            {
                                ShowWindow(hwnd2, SHOW_WINDOW_CMD.SW_RESTORE);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore restore errors
                }
            });
        }

        private void OnWindow2Destroyed(IntPtr hwnd)
        {
            WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
            Detach();
        }

        private void UpdatePosition()
        {
            if (!_isAttached || _isUpdatingPosition)
                return;

            if (!IsWindow(_window1Handle) || !IsWindow(_window2Handle))
            {
                WindowDestroyed?.Invoke(_window1Handle.Value, _window2Handle.Value);
                Detach();
                return;
            }

            _isUpdatingPosition = true;

            try
            {
                bool isWindow2ToolWindow = WindowHelper.IsToolWindow(_window2Handle.Value);
                bool isWindow1Hidden = IsIconic(_window1Handle) || !IsWindowVisible(_window1Handle);

                if (isWindow1Hidden)
                {
                    if (isWindow2ToolWindow)
                    {
                        if (IsWindowVisible(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_HIDE);
                        }
                    }
                    else
                    {
                        if (!IsIconic(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_MINIMIZE);
                        }
                    }
                    return;
                }
                else
                {
                    if (isWindow2ToolWindow)
                    {
                        if (!IsWindowVisible(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                        }
                    }
                    else
                    {
                        if (IsIconic(_window2Handle))
                        {
                            ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                        }
                    }
                }

                var window1Rect = WindowHelper.GetWindowRect(_window1Handle.Value);
                if (window1Rect == null)
                    return;

                var window2Rect = WindowHelper.GetWindowRect(_window2Handle.Value);
                if (window2Rect == null)
                    return;

                int window1Left = window1Rect.Value.Left;
                int window1Top = window1Rect.Value.Top;
                int window1Width = window1Rect.Value.Width;
                int window1Height = window1Rect.Value.Height;
                int window2Width = window2Rect.Value.Width;
                int window2Height = window2Rect.Value.Height;

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
                }

                // Auto-optimize position to maximize visible area if enabled (excluding overlap with window1)
                // Tries all placement positions and selects the one with maximum visible area
                // Prioritizes opposite placement (e.g., LeftTop -> RightTop)
                if (_autoOptimizePosition)
                {
                    var (adjustedX, adjustedY) = ScreenAdjustHelper.AdjustPositionToScreen(
                        window2X, window2Y, window2Width, window2Height, _window2Handle.Value, window1Rect.Value, _placement, _offsetX, _offsetY);
                    window2X = adjustedX;
                    window2Y = adjustedY;
                }

                // Restrict to same screen if enabled
                if (_restrictToSameScreen)
                {
                    var workArea = WindowHelper.GetMonitorWorkArea(_window1Handle.Value);
                    if (workArea.HasValue)
                    {
                        if (window2X < workArea.Value.Left)
                            window2X = workArea.Value.Left;
                        if (window2Y < workArea.Value.Top)
                            window2Y = workArea.Value.Top;
                        if (window2X + window2Width > workArea.Value.Right)
                            window2X = workArea.Value.Right - window2Width;
                        if (window2Y + window2Height > workArea.Value.Bottom)
                            window2Y = workArea.Value.Bottom - window2Height;
                    }
                }

                WindowHelper.SetWindowPos(_window2Handle.Value, window2X, window2Y, window2Width, window2Height,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        private void Detach()
        {
            if (_isAttached && _window2Handle.Value != IntPtr.Zero)
            {
                bool window2Exists = IsWindow(_window2Handle);
                
                if (window2Exists)
                {
                    if (_originalToolWindowState.HasValue)
                    {
                        WindowHelper.SetWindowExStyle(_window2Handle.Value, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW, _originalToolWindowState.Value);
                    }
                    
                    WindowHelper.SetWindowOwner(_window2Handle.Value, IntPtr.Zero);
                    
                    bool isWindow2Hidden = !IsWindowVisible(_window2Handle);
                    bool isWindow2Minimized = IsIconic(_window2Handle);
                    
                    if (isWindow2Hidden && !isWindow2Minimized)
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_SHOWNA);
                    }
                    else if (isWindow2Minimized)
                    {
                        ShowWindow(_window2Handle, SHOW_WINDOW_CMD.SW_RESTORE);
                    }
                }
            }

            _isAttached = false;
            StopEventHooks();
            _callbackAction = null;
            _originalToolWindowState = null;
            _window1Handle = HWND.Null;
            _window2Handle = HWND.Null;
        }

        public void Dispose()
        {
            Detach();
        }
    }
}

