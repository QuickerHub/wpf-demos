using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowEdgeHide.Interfaces;
using WindowEdgeHide.Models;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Service for managing window edge hiding behavior
    /// Only hides window when it's at screen edge, and determines hide direction based on current position
    /// </summary>
    public class WindowEdgeHideService : IDisposable
    {
        private readonly HWND _windowHandle;
        private readonly IntThickness _visibleArea;
        private readonly ActivationStrategy _activationStrategy; // Window activation strategy
        private readonly EdgeDirection _updateEdgeDirection; // Edge direction for restore/update
        private bool _isEnabled;
        private bool _isHidden = false;
        private EdgeDirection _currentHideDirection = EdgeDirection.Nearest; // Current hide direction
        private WindowRect? _originalPosition; // Store original position before hiding
        private readonly bool? _originalTopmost; // Store original topmost state before enabling
        private IWindowMover _mover; // Single mover for both hiding and showing (prevents animation conflicts)
        private readonly WindowMouseHook _mouseHook; // Mouse hook for monitoring mouse enter/leave
        private readonly ManagedWindow _managedWindow; // Managed window for state monitoring

        /// <summary>
        /// Event raised when window is destroyed
        /// </summary>
        public event Action<IntPtr>? WindowDestroyed;

        /// <summary>
        /// Initialize window edge hiding
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="edgeDirection">Edge direction to attach to. If Nearest, automatically selects nearest edge.</param>
        /// <param name="visibleArea">Visible area thickness when hidden (default: all sides 5)</param>
        /// <param name="mover">Window mover for animation (default: DirectWindowMover). Use same mover for hide/show to prevent conflicts.</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        /// <param name="activationStrategy">Window activation strategy when showing from edge hide (default: AutoActivate)</param>
        /// <param name="updateEdgeDirection">Edge direction for window restore/update. If None, automatically selects nearest edge (default: None)</param>
        public WindowEdgeHideService(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest, 
            IntThickness visibleArea = default, 
            IWindowMover? mover = null, bool showOnScreenEdge = false, ActivationStrategy activationStrategy = ActivationStrategy.AutoActivate, EdgeDirection updateEdgeDirection = EdgeDirection.None)
        {
            var hwnd = new HWND(windowHandle);
            if (!IsWindow(hwnd))
                throw new ArgumentException("Invalid window handle");

            _windowHandle = hwnd;
            _activationStrategy = activationStrategy;
            _updateEdgeDirection = updateEdgeDirection;
            // Use default thickness if not specified
            if (visibleArea.Equals(default(IntThickness)))
            {
                _visibleArea = new IntThickness(5);
            }
            else
            {
                _visibleArea = visibleArea;
            }
            _isEnabled = true;
            
            // Set window mover (default to direct move)
            // Use single mover for both hide and show to prevent animation conflicts
            _mover = mover ?? new Implementations.DirectWindowMover();

            // Determine actual edge direction
            EdgeDirection actualDirection = edgeDirection;
            if (edgeDirection == EdgeDirection.Nearest)
            {
                actualDirection = EdgeCalculator.FindNearestEdge(windowHandle);
            }

            // Only move window to screen edge if edgeDirection is not None
            if (edgeDirection != EdgeDirection.None)
            {
            // Always move window to screen edge (even if already at edge, ensure correct position)
            var edgePos = EdgeCalculator.CalculateEdgePosition(windowHandle, actualDirection, _visibleArea);
            var directMover = new Implementations.DirectWindowMover();
            directMover.MoveWindow(windowHandle, edgePos.x, edgePos.y, edgePos.width, edgePos.height);

            // Store edge position as original (where window should be restored to)
            _originalPosition = new WindowRect(edgePos.x, edgePos.y, edgePos.x + edgePos.width, edgePos.y + edgePos.height);
            }
            else
            {
                // For None option, store current position as original (don't move window)
                var currentRect = WindowHelper.GetWindowRect(windowHandle);
                if (currentRect != null)
                {
                    _originalPosition = currentRect.Value;
                }
            }
            
            // Ensure we start in visible state
            _isHidden = false;

            // Record original topmost state (only if activation strategy requires topmost)
            _originalTopmost = (_activationStrategy == ActivationStrategy.AutoActivate || _activationStrategy == ActivationStrategy.Topmost) 
                ? WindowHelper.GetWindowTopmost(windowHandle) : null;

            // Create managed window for state monitoring
            _managedWindow = new ManagedWindow(windowHandle);
            _managedWindow.IsActiveChanged += ManagedWindow_IsActiveChanged;
            _managedWindow.WindowStateChanged += ManagedWindow_WindowStateChanged;
            
            // Set topmost based on activation strategy
            // Important: Never set topmost for focused windows
            bool isFocused = _managedWindow.IsActive;
            
            if (_activationStrategy == ActivationStrategy.Topmost)
            {
                // Topmost strategy: set topmost only if window is not focused
                if (!isFocused)
                {
                    _managedWindow.Topmost = true;
                }
            }
            else if (_activationStrategy == ActivationStrategy.AutoActivate)
            {
                // AutoActivate strategy: set topmost only if window is not focused
                // Focused windows will use activation strategy, unfocused windows use topmost
                if (!isFocused)
                {
                    _managedWindow.Topmost = true;
                }
            }
            // None strategy: do nothing

            // Create and start mouse hook
            _mouseHook = new WindowMouseHook(windowHandle, _visibleArea, showOnScreenEdge);
            _mouseHook.MouseLeave += MouseHook_MouseLeave;
            _mouseHook.MouseEnter += MouseHook_MouseEnter;
            _mouseHook.Start();

            // Force hide on first enable if window is at screen edge and mouse is not over window
            // Skip if window is minimized
            if (!_managedWindow.IsMinimized())
            {
                var cursorPos = WindowHelper.GetCursorPos();
                if (cursorPos != null)
                {
                    var windowRect = WindowHelper.GetWindowRect(windowHandle);
                    if (windowRect != null)
                    {
                        bool isCursorOverWindow = WindowHelper.IsPointInRect(cursorPos.Value, windowRect.Value);
                        bool isAtEdge = EdgeCalculator.IsWindowAtEdge(windowHandle);
                        
                        if (isAtEdge && !isCursorOverWindow)
                        {
                            // Window is at edge and mouse is not over it, hide immediately
                            HideWindow();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unregister window edge hiding
        /// </summary>
        public void Unregister()
        {
            // Set disabled flag first to prevent new operations
            _isEnabled = false;
            
            // Restore window to original position if hidden (do this before disposing hooks)
            if (_isHidden && _originalPosition != null && _windowHandle.Value != IntPtr.Zero)
            {
                var pos = _originalPosition.Value;
                _mover?.MoveWindow(_windowHandle.Value, pos.Left, pos.Top, pos.Width, pos.Height);
            }

            // Restore original topmost state (if activation strategy required topmost)
            if ((_activationStrategy == ActivationStrategy.AutoActivate || _activationStrategy == ActivationStrategy.Topmost) 
                && _originalTopmost.HasValue && _windowHandle.Value != IntPtr.Zero)
            {
                WindowHelper.SetWindowTopmost(_windowHandle.Value, _originalTopmost.Value);
            }

            // Stop and dispose hooks (they will clean up their own event subscriptions)
            _mouseHook.Stop();
            _mouseHook.Dispose();

            // Dispose managed window (it will clean up its own event subscriptions and unhook WinEvent hooks)
            _managedWindow.Dispose();

            // Reset state
            _isHidden = false;
        }

        /// <summary>
        /// Restore window to original position (show window)
        /// Calculates the edge position based on current hidden state and restores to that position
        /// Uses focus-aware activation strategy if enabled:
        /// - Focused windows: auto-activate after show
        /// - Unfocused windows: use Topmost=true without activation
        /// </summary>
        private void RestorePosition()
        {
            if (_windowHandle.Value == IntPtr.Zero || !_isEnabled)
                return;

            // Skip if window is minimized
            if (_managedWindow.IsMinimized())
                return;

            // Always restore to edge position (ensure window is at edge after restore)
            // This is critical for MouseLeave to work correctly
            EdgeDirection edgeDirection;
            if (_updateEdgeDirection == EdgeDirection.None)
            {
                // Auto-detect nearest edge
                edgeDirection = EdgeCalculator.FindNearestEdge(_windowHandle.Value);
            }
            else
            {
                // Use configured update edge direction
                edgeDirection = _updateEdgeDirection;
            }
            var edgePos = EdgeCalculator.CalculateEdgePosition(_windowHandle.Value, edgeDirection, _visibleArea);
            
            // Restore to edge position
            _mover.MoveWindow(_windowHandle.Value, edgePos.x, edgePos.y, edgePos.width, edgePos.height);
            
            // Update _originalPosition to the restored edge position
            _originalPosition = new WindowRect(edgePos.x, edgePos.y, edgePos.x + edgePos.width, edgePos.y + edgePos.height);
            
            // Apply activation strategy
            switch (_activationStrategy)
            {
                case ActivationStrategy.AutoActivate:
                    // Auto-activate window when showing
                    // For noactive windows, automatically fallback to Topmost
                    bool isNoActive = WindowHelper.IsNoActiveWindow(_windowHandle.Value);
                    if (isNoActive)
                    {
                        // Noactive window: fallback to Topmost strategy
                        _managedWindow.Topmost = true;
                    }
                    else
                    {
                        // Normal window: check if it has focus
                        var foregroundWindow = WindowHelper.GetForegroundWindow();
                        bool isFocused = foregroundWindow == _windowHandle.Value;
                        
                        if (isFocused)
                        {
                            // Window already has focus: clear topmost and ensure it's visible
                            // For focused windows, we don't want topmost, just ensure visibility
                            _managedWindow.Topmost = false;
                            // Ensure window is visible and bring to front without stealing focus
                            var hwnd = new Windows.Win32.Foundation.HWND(_windowHandle.Value);
                            // Use SetWindowPos to bring to front without activation (SWP_NOACTIVATE)
                            Windows.Win32.PInvoke.SetWindowPos(
                                hwnd,
                                new Windows.Win32.Foundation.HWND(new IntPtr(0)), // HWND_TOP (but not topmost)
                                0, 0, 0, 0,
                                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                        }
                        else
                        {
                            // Window doesn't have focus: activate it and clear topmost
                            // Clear topmost first (if it was set during initialization)
                            _managedWindow.Topmost = false;
                            // Then activate the window
                            WindowHelper.ActivateWindow(_windowHandle.Value);
                        }
                    }
                    break;

                case ActivationStrategy.Topmost:
                    // Set window to topmost without activation
                    // But never set topmost for focused windows
                    var foregroundWindowForTopmost = WindowHelper.GetForegroundWindow();
                    bool isFocusedForTopmost = foregroundWindowForTopmost == _windowHandle.Value;
                    if (!isFocusedForTopmost)
                    {
                        _managedWindow.Topmost = true;
                    }
                    break;

                case ActivationStrategy.None:
                    // Do nothing (no activation, no topmost)
                    break;
            }
            
            _isHidden = false;
        }

        /// <summary>
        /// Hide window based on current position (determine direction automatically)
        /// Directly animates from current position to hidden position
        /// </summary>
        private void HideWindow()
        {
            if (_windowHandle.Value == IntPtr.Zero || !_isEnabled)
                return;

            // Skip if window is minimized
            if (_managedWindow.IsMinimized())
                return;

            // Get current window position before hiding
            var currentRect = WindowHelper.GetWindowRect(_windowHandle.Value);
            if (currentRect == null)
                return;

            // Determine hide direction based on current position
            EdgeDirection hideDirection = EdgeCalculator.FindNearestEdge(_windowHandle.Value);
            _currentHideDirection = hideDirection; // Update current hide direction

            // Calculate edge position (where window should be restored to when shown)
            var edgePos = EdgeCalculator.CalculateEdgePosition(_windowHandle.Value, hideDirection, _visibleArea);
            
            // Update original position to the edge position (where window should be restored to)
            // This ensures that when window is shown, it returns to the edge, not the initial position
            _originalPosition = new WindowRect(edgePos.x, edgePos.y, edgePos.x + edgePos.width, edgePos.y + edgePos.height);
            
            // Calculate hidden position based on current position (directly animate from current to hidden)
            var hiddenPos = EdgeCalculator.CalculateHiddenPosition(_windowHandle.Value, hideDirection, _visibleArea);
            
            // Directly animate from current position to hidden position
            _mover.MoveWindow(_windowHandle.Value, hiddenPos.x, hiddenPos.y, hiddenPos.width, hiddenPos.height);
            _isHidden = true;
        }

        /// <summary>
        /// Set window mover for animation (used for both hiding and showing)
        /// </summary>
        /// <param name="mover">Window mover implementation</param>
        public void SetMover(IWindowMover mover)
        {
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        }

        /// <summary>
        /// Mouse leave event handler
        /// </summary>
        private void MouseHook_MouseLeave(IntPtr windowHandle)
        {
            if (!_isEnabled || _windowHandle.Value != windowHandle)
                return;

            // Check if window still exists
            if (!IsWindow(_windowHandle))
            {
                WindowDestroyed?.Invoke(_windowHandle.Value);
                Unregister();
                return;
            }

            // Mouse left the window, hide if window is at screen edge
            // Skip if window is minimized or activated (user is interacting with it)
            if (!_isHidden && !_managedWindow.IsMinimized() && !_managedWindow.IsActive)
            {
                // Check if window is at screen edge
                bool shouldHide = EdgeCalculator.IsWindowAtEdge(_windowHandle.Value);
                if (shouldHide)
                {
                    HideWindow();
                }
            }
        }

        /// <summary>
        /// Mouse enter event handler
        /// </summary>
        private void MouseHook_MouseEnter(IntPtr windowHandle)
        {
            if (!_isEnabled || _windowHandle.Value != windowHandle)
                return;

            // Mouse entered the window, restore if hidden
            // Skip if window is minimized
            if (_isHidden && !_managedWindow.IsMinimized())
            {
                RestorePosition();
            }
        }

        /// <summary>
        /// Managed window IsActive changed handler
        /// </summary>
        private void ManagedWindow_IsActiveChanged(object? sender, bool isActive)
        {
            if (!_isEnabled)
                return;

            if (isActive)
            {
                // Window was activated, restore if hidden
                if (_isHidden)
                {
                    RestorePosition();
                }
            }
            else
            {
                // Window was deactivated, hide if mouse is not over window and window is at screen edge
                // Skip if window is minimized
                if (!_isHidden && !_managedWindow.IsMinimized())
                {
                    // Check if mouse is over window
                    var cursorPos = WindowHelper.GetCursorPos();
                    if (cursorPos != null)
                    {
                        var windowRect = WindowHelper.GetWindowRect(_windowHandle.Value);
                        if (windowRect != null)
                        {
                            bool isCursorOverWindow = WindowHelper.IsPointInRect(cursorPos.Value, windowRect.Value);
                            
                            // Only hide if mouse is not over window and window is at screen edge
                            if (!isCursorOverWindow)
                            {
                                bool shouldHide = EdgeCalculator.IsWindowAtEdge(_windowHandle.Value);
                                if (shouldHide)
                                {
                                    HideWindow();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Managed window WindowState changed handler
        /// </summary>
        private void ManagedWindow_WindowStateChanged(object? sender, Models.WindowState newState)
        {
            // If window becomes minimized, ensure we don't try to hide/show
            // If window becomes normal/maximized and was hidden, we might need to restore
            // But typically we let mouse/activation events handle this
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}

