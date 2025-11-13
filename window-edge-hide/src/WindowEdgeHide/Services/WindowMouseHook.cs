using System;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowEdgeHide.Models;
using WindowEdgeHide.Utils;
using EdgeDirection = WindowEdgeHide.Models.EdgeDirection;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Mouse hook to monitor mouse position relative to a window
    /// Uses DispatcherTimer to check mouse position periodically
    /// Considers visible area when window is hidden
    /// </summary>
    internal class WindowMouseHook : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly IntThickness _visibleArea;
        private DispatcherTimer? _timer;
        private bool _isMouseOverWindow;
#if DEBUG
        private TriggerAreaDebugWindow? _debugWindow;
#endif

        /// <summary>
        /// Threshold in pixels for detecting mouse near window (default: 5)
        /// </summary>
        public int EdgeThreshold { get; set; } = 5;

        /// <summary>
        /// If true, show window when mouse is at screen edge (default: false)
        /// If false, only show when mouse is near the window itself
        /// </summary>
        public bool ShowOnScreenEdge { get; set; } = false;

        /// <summary>
        /// Event raised when mouse enters the window
        /// </summary>
        public event Action<IntPtr>? MouseEnter;

        /// <summary>
        /// Event raised when mouse leaves the window
        /// </summary>
        public event Action<IntPtr>? MouseLeave;

        /// <summary>
        /// Initialize mouse hook for a window
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="visibleArea">Visible area thickness when hidden</param>
        /// <param name="showOnScreenEdge">If true, show window when mouse is at screen edge (default: false)</param>
        public WindowMouseHook(IntPtr windowHandle, IntThickness visibleArea, bool showOnScreenEdge = false)
        {
            _windowHandle = windowHandle;
            _visibleArea = visibleArea;
            EdgeThreshold = visibleArea.Left; // Use left thickness as default threshold
            ShowOnScreenEdge = showOnScreenEdge;
            var hwnd = new HWND(windowHandle);
            if (!IsWindow(hwnd))
                throw new ArgumentException("Invalid window handle");
        }

        /// <summary>
        /// Start monitoring mouse events
        /// </summary>
        public void Start()
        {
            if (_timer != null)
                return;

            _isMouseOverWindow = false;

            _timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(50) // Check every 50ms
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

#if DEBUG
            // Create debug window
            _debugWindow = new TriggerAreaDebugWindow();
            _debugWindow.Show();
#endif

            // Initial check
            CheckMousePosition();
        }

        /// <summary>
        /// Stop monitoring mouse events
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
            _isMouseOverWindow = false;

#if DEBUG
            // Close debug window
            if (_debugWindow != null)
            {
                _debugWindow.Close();
                _debugWindow = null;
            }
#endif
        }

        /// <summary>
        /// Timer tick handler
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            CheckMousePosition();
            
            // Also check if mouse is near screen edge (for hidden windows)
            // This is handled by the service through a separate mechanism
        }

        /// <summary>
        /// Calculate the mouse detection area for the window
        /// Simply: window-screen intersection, then shrink by visibleArea (negative values expand), then expand by 3 pixels
        /// </summary>
        /// <param name="windowRect">Full window rectangle from GetWindowRect</param>
        /// <param name="screenRect">Screen work area rectangle</param>
        /// <returns>Mouse detection area (expanded by 3 pixels)</returns>
        private WindowRect CalculateDetectionArea(WindowRect windowRect, WindowRect screenRect)
        {
            // Get intersection of window and screen
            var intersection = windowRect.Intersect(screenRect);
            
            // Shrink by visibleArea (negative values will expand outward)
            var triggerArea = intersection.Shrink(_visibleArea);
            
            // Expand by 3 pixels for robustness
            var expandedTriggerArea = triggerArea.Expand(3);
            
            return expandedTriggerArea;
        }

        /// <summary>
        /// Check mouse position and trigger events
        /// </summary>
        private void CheckMousePosition()
        {
            if (_windowHandle == IntPtr.Zero)
                return;

            var hwnd = new HWND(_windowHandle);
            if (!IsWindow(hwnd))
            {
                Stop();
                return;
            }

            // Get cursor position
            var cursorPos = WindowHelper.GetCursorPos();
            if (cursorPos == null)
                return;

            // Get window rectangle
            var windowRect = WindowHelper.GetWindowRect(_windowHandle);
            if (windowRect == null)
                return;

            // Get screen rectangle
            var screenRect = WindowHelper.GetMonitorWorkArea(_windowHandle);
            if (screenRect == null)
                return;

            // Calculate mouse detection area (already expanded by 3 pixels)
            var detectionArea = CalculateDetectionArea(windowRect.Value, screenRect.Value);

#if DEBUG
            // Update debug window
            if (_debugWindow != null)
            {
                _debugWindow.UpdateTriggerArea(detectionArea);
            }
#endif

            // Check if cursor is over detection area
            bool isOverWindow = detectionArea.Contains(cursorPos.Value);

            // Extended check: if mouse is not directly over detection area, check if it's near the detection area
            if (!isOverWindow)
            {
                int threshold = EdgeThreshold;

                // Check if mouse is near the detection area (expanded by threshold)
                isOverWindow = detectionArea.IsNear(cursorPos.Value, threshold);

                // If ShowOnScreenEdge is enabled, also check screen edge
                if (!isOverWindow && ShowOnScreenEdge)
                {
                    var screen = screenRect.Value;

                    // Check if mouse is near any screen edge and window is at that edge
                    cursorPos.Value.CheckNearEdges(screen, threshold, out bool nearLeft, out bool nearTop, out bool nearRight, out bool nearBottom);
                    windowRect.Value.CheckEdges(screen, threshold, out bool windowAtLeft, out bool windowAtTop, out bool windowAtRight, out bool windowAtBottom);

                    // If mouse is near the edge where window is located, consider it as mouse over window
                    isOverWindow = (nearLeft && windowAtLeft) ||
                                 (nearTop && windowAtTop) ||
                                 (nearRight && windowAtRight) ||
                                 (nearBottom && windowAtBottom);
                }
            }

            // Trigger events on state change
            if (isOverWindow && !_isMouseOverWindow)
            {
                _isMouseOverWindow = true;
                MouseEnter?.Invoke(_windowHandle);
            }
            else if (!isOverWindow && _isMouseOverWindow)
            {
                _isMouseOverWindow = false;
                MouseLeave?.Invoke(_windowHandle);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
