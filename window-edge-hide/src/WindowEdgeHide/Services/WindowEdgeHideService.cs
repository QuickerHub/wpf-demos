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
        private HWND _windowHandle;
        private IntThickness _visibleArea;
        private bool _isEnabled;
        private bool _isHidden = false;
        private EdgeDirection _currentHideDirection = EdgeDirection.Nearest; // Current hide direction
        private WindowRect? _originalPosition; // Store original position before hiding
        private bool? _originalTopmost; // Store original topmost state before enabling
        private IWindowMover _mover = new Implementations.DirectWindowMover(); // Single mover for both hiding and showing (prevents animation conflicts)
        private WindowMouseHook? _mouseHook; // Mouse hook for monitoring mouse enter/leave
        private ManagedWindow? _managedWindow; // Managed window for state monitoring

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
        public void Enable(IntPtr windowHandle, EdgeDirection edgeDirection = EdgeDirection.Nearest, 
            IntThickness visibleArea = default, 
            IWindowMover? mover = null, bool showOnScreenEdge = false)
        {
            var hwnd = new HWND(windowHandle);
            if (!IsWindow(hwnd))
                throw new ArgumentException("Invalid window handle");

            Disable();

            _windowHandle = hwnd;
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

            // Always move window to screen edge (even if already at edge, ensure correct position)
            var edgePos = EdgeCalculator.CalculateEdgePosition(windowHandle, actualDirection, _visibleArea);
            var directMover = new Implementations.DirectWindowMover();
            directMover.MoveWindow(windowHandle, edgePos.x, edgePos.y, edgePos.width, edgePos.height);

            // Store edge position as original (where window should be restored to)
            _originalPosition = new WindowRect(edgePos.x, edgePos.y, edgePos.x + edgePos.width, edgePos.y + edgePos.height);
            
            // Ensure we start in visible state
            _isHidden = false;

            // Record original topmost state
            _originalTopmost = WindowHelper.GetWindowTopmost(windowHandle);

            // Create managed window for state monitoring
            _managedWindow = new ManagedWindow(windowHandle);
            _managedWindow.IsActiveChanged += ManagedWindow_IsActiveChanged;
            _managedWindow.WindowStateChanged += ManagedWindow_WindowStateChanged;

            // Set window to topmost
            _managedWindow.Topmost = true;

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
        /// Disable window edge hiding
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;
            
            // Stop mouse hook
            if (_mouseHook != null)
            {
                _mouseHook.MouseLeave -= MouseHook_MouseLeave;
                _mouseHook.MouseEnter -= MouseHook_MouseEnter;
                _mouseHook.Stop();
                _mouseHook.Dispose();
                _mouseHook = null;
            }

            // Stop managed window
            if (_managedWindow != null)
            {
                _managedWindow.IsActiveChanged -= ManagedWindow_IsActiveChanged;
                _managedWindow.WindowStateChanged -= ManagedWindow_WindowStateChanged;
                _managedWindow.Dispose();
                _managedWindow = null;
            }

            // Restore window to original position if hidden
            if (_isHidden && _originalPosition != null && _windowHandle.Value != IntPtr.Zero)
            {
                var pos = _originalPosition.Value;
                _mover?.MoveWindow(_windowHandle.Value, pos.Left, pos.Top, pos.Width, pos.Height);
            }

            // Restore original topmost state
            if (_originalTopmost.HasValue && _windowHandle.Value != IntPtr.Zero)
            {
                WindowHelper.SetWindowTopmost(_windowHandle.Value, _originalTopmost.Value);
            }

            _windowHandle = HWND.Null;
            _isHidden = false;
            _originalPosition = null;
            _originalTopmost = null;
        }

        /// <summary>
        /// Restore window to original position (show window)
        /// Calculates the edge position based on current hidden state and restores to that position
        /// </summary>
        private void RestorePosition()
        {
            if (_windowHandle.Value == IntPtr.Zero || _managedWindow == null)
                return;

            // Skip if window is minimized
            if (_managedWindow.IsMinimized())
                return;

            // If we have a stored original position, use it
            if (_originalPosition != null)
            {
                var pos = _originalPosition.Value;
                _mover.MoveWindow(_windowHandle.Value, pos.Left, pos.Top, pos.Width, pos.Height);
                _isHidden = false;
                return;
            }

            // Otherwise, calculate edge position based on current window position
            var currentRect = WindowHelper.GetWindowRect(_windowHandle.Value);
            if (currentRect == null)
                return;

            // Determine edge direction based on current position
            EdgeDirection edgeDirection = EdgeCalculator.FindNearestEdge(_windowHandle.Value);
            
            // Calculate edge position to restore to
            var edgePos = EdgeCalculator.CalculateEdgePosition(_windowHandle.Value, edgeDirection, _visibleArea);
            
            // Restore to edge position
            _mover.MoveWindow(_windowHandle.Value, edgePos.x, edgePos.y, edgePos.width, edgePos.height);
            _isHidden = false;
        }

        /// <summary>
        /// Hide window based on current position (determine direction automatically)
        /// Directly animates from current position to hidden position
        /// </summary>
        private void HideWindow()
        {
            if (_windowHandle.Value == IntPtr.Zero)
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
                Disable();
                return;
            }

            // Mouse left the window, hide if window is at screen edge AND window is not activated
            // Skip if window is minimized
            if (!_isHidden && !_managedWindow.IsMinimized())
            {
                // Check if window is at screen edge
                bool shouldHide = EdgeCalculator.IsWindowAtEdge(_windowHandle.Value);
                
                // Also check if window is not activated (not foreground)
                if (shouldHide && !_managedWindow.IsActive)
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
            if (!_isEnabled || _managedWindow == null)
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
            Disable();
        }
    }
}

