using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace QuickerCodeEditor.View
{
    /// <summary>
    /// A Popup that permanently follows a window, automatically handling position, size, and state changes
    /// </summary>
    public class WindowAttachedPopup : Popup
    {
        private Window? _targetWindow;
        private HwndSource? _popupHwndSource;
        private bool _isSubscribed = false;
        private bool _isRefreshing = false;
        private bool _isFirstOpen = true;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
        }

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Target window to follow
        /// </summary>
        public Window? TargetWindow
        {
            get => _targetWindow;
            set
            {
                if (_targetWindow != null)
                {
                    UnsubscribeFromWindow(_targetWindow);
                }
                _targetWindow = value;
                if (_targetWindow != null)
                {
                    SubscribeToWindow(_targetWindow);
                }
            }
        }

        /// <summary>
        /// Horizontal offset from the target window
        /// </summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>
        /// Vertical offset from the target window
        /// </summary>
        public double OffsetY { get; set; } = 0;

        /// <summary>
        /// Placement relative to the target window
        /// </summary>
        public WindowPlacement WindowPlacement { get; set; } = WindowPlacement.Right;

        public WindowAttachedPopup()
        {
            this.StaysOpen = true;
            this.AllowsTransparency = true;
            this.Opened += WindowAttachedPopup_Opened;
            this.Closed += WindowAttachedPopup_Closed;
        }

        private void WindowAttachedPopup_Opened(object? sender, EventArgs e)
        {
            // Prevent infinite loop during refresh
            if (_isRefreshing)
            {
                _isRefreshing = false;
                _isFirstOpen = false; // Mark as not first open after refresh
                return;
            }
            
            // Get popup's HwndSource when it's opened
            GetPopupHwndSource();
            SetPopupTopmost();
            
            // Update position after popup is opened
            if (TargetWindow != null)
            {
                UpdatePosition();
                
                // Force refresh by toggling IsOpen to ensure content is fully rendered (only on first open)
                if (_isFirstOpen)
                {
                    TargetWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (this.IsOpen && !_isRefreshing)
                        {
                            _isRefreshing = true;
                            this.IsOpen = false;
                            TargetWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                            {
                                this.IsOpen = true;
                                UpdatePosition();
                                SetPopupTopmost();
                            }));
                        }
                    }));
                }
            }
        }

        private void WindowAttachedPopup_Closed(object? sender, EventArgs e)
        {
            _popupHwndSource = null;
        }

        private void SubscribeToWindow(Window window)
        {
            if (_isSubscribed)
                return;

            window.LocationChanged += Window_LocationChanged;
            window.SizeChanged += Window_SizeChanged;
            window.StateChanged += Window_StateChanged;
            window.Activated += Window_Activated;
            window.Deactivated += Window_Deactivated;
            window.Loaded += Window_Loaded;

            _isSubscribed = true;

            // If window is already loaded, setup immediately
            if (window.IsLoaded)
            {
                SetupPlacementTarget();
            }
        }

        private void UnsubscribeFromWindow(Window window)
        {
            if (!_isSubscribed)
                return;

            window.LocationChanged -= Window_LocationChanged;
            window.SizeChanged -= Window_SizeChanged;
            window.StateChanged -= Window_StateChanged;
            window.Activated -= Window_Activated;
            window.Deactivated -= Window_Deactivated;
            window.Loaded -= Window_Loaded;

            _isSubscribed = false;
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            SetupPlacementTarget();
        }

        private void SetupPlacementTarget()
        {
            if (TargetWindow == null || !TargetWindow.IsLoaded)
                return;

            if (this.PlacementTarget == null)
            {
                this.PlacementTarget = TargetWindow.Content as FrameworkElement ?? TargetWindow;
            }
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            if (this.IsOpen)
            {
                UpdatePosition();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsOpen)
            {
                UpdatePosition();
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (TargetWindow == null)
                return;

            if (TargetWindow.WindowState == WindowState.Minimized)
            {
                this.IsOpen = false;
            }
            else if (TargetWindow.IsActive)
            {
                // Show popup if window is active and not minimized
                ShowPopup();
            }
        }

        private void Window_Activated(object? sender, EventArgs e)
        {
            if (TargetWindow?.WindowState != WindowState.Minimized)
            {
                ShowPopup();
            }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            this.IsOpen = false;
        }

        private void ShowPopup()
        {
            if (TargetWindow == null || TargetWindow.WindowState == WindowState.Minimized)
                return;

            // Use Dispatcher to ensure UI is ready
            TargetWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (TargetWindow == null || TargetWindow.WindowState == WindowState.Minimized)
                    return;

                // Force popup to open
                if (!this.IsOpen)
                {
                    this.IsOpen = true;
                }

                // Update position and set topmost
                UpdatePosition();
                SetPopupTopmost();
            }));
        }

        private void UpdatePosition()
        {
            if (TargetWindow == null || !TargetWindow.IsLoaded || !TargetWindow.IsVisible)
                return;

            var source = PresentationSource.FromVisual(TargetWindow);
            if (source == null)
                return;

            var hwndSource = source as HwndSource;
            if (hwndSource == null || hwndSource.Handle == IntPtr.Zero)
                return;

            // Get window's monitor (screen) first
            var monitor = MonitorFromWindow(hwndSource.Handle, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
                return;

            var monitorInfo = new MONITORINFO { Size = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
                return;

            RECT windowRect;
            if (!GetWindowRect(hwndSource.Handle, out windowRect))
                return;

            // Get DPI scale factor for the window's screen
            var compositionTarget = source.CompositionTarget;
            if (compositionTarget == null)
                return;

            var dpiScaleX = compositionTarget.TransformToDevice.M11;
            var dpiScaleY = compositionTarget.TransformToDevice.M22;

            if (dpiScaleX <= 0 || dpiScaleY <= 0)
                return;

            // Convert physical pixels to device-independent pixels
            double windowLeft = windowRect.Left / dpiScaleX;
            double windowTop = windowRect.Top / dpiScaleY;
            double windowWidth = (windowRect.Right - windowRect.Left) / dpiScaleX;
            double windowHeight = (windowRect.Bottom - windowRect.Top) / dpiScaleY;

            // Get screen work area in device-independent pixels
            double screenLeft = monitorInfo.WorkArea.Left / dpiScaleX;
            double screenTop = monitorInfo.WorkArea.Top / dpiScaleY;
            double screenRight = monitorInfo.WorkArea.Right / dpiScaleX;
            double screenBottom = monitorInfo.WorkArea.Bottom / dpiScaleY;

            // Calculate popup position based on placement
            double popupX = 0, popupY = 0;

            switch (WindowPlacement)
            {
                case WindowPlacement.Left:
                    popupX = windowLeft - this.Width - OffsetX;
                    popupY = windowTop + OffsetY;
                    break;
                case WindowPlacement.Right:
                    popupX = windowLeft + windowWidth + OffsetX;
                    popupY = windowTop + OffsetY;
                    break;
                case WindowPlacement.Top:
                    popupX = windowLeft + OffsetX;
                    popupY = windowTop - this.Height - OffsetY;
                    break;
                case WindowPlacement.Bottom:
                    popupX = windowLeft + OffsetX;
                    popupY = windowTop + windowHeight + OffsetY;
                    break;
                case WindowPlacement.TopLeft:
                    popupX = windowLeft + OffsetX;
                    popupY = windowTop + OffsetY;
                    break;
                case WindowPlacement.TopRight:
                    popupX = windowLeft + windowWidth - this.Width - OffsetX;
                    popupY = windowTop + OffsetY;
                    break;
                case WindowPlacement.BottomLeft:
                    popupX = windowLeft + OffsetX;
                    popupY = windowTop + windowHeight - this.Height - OffsetY;
                    break;
                case WindowPlacement.BottomRight:
                    popupX = windowLeft + windowWidth - this.Width - OffsetX;
                    popupY = windowTop + windowHeight - this.Height - OffsetY;
                    break;
            }

            // Constrain popup position to screen bounds (work area)
            if (popupX < screenLeft)
                popupX = screenLeft;
            if (popupY < screenTop)
                popupY = screenTop;
            if (popupX + this.Width > screenRight)
                popupX = screenRight - this.Width;
            if (popupY + this.Height > screenBottom)
                popupY = screenBottom - this.Height;

            // Use Absolute placement mode
            this.Placement = PlacementMode.Absolute;
            this.HorizontalOffset = popupX;
            this.VerticalOffset = popupY;
        }

        private void GetPopupHwndSource()
        {
            if (_popupHwndSource != null && _popupHwndSource.Handle != IntPtr.Zero)
                return;

            // Try to get HwndSource from the popup's child
            if (this.Child != null)
            {
                var source = PresentationSource.FromVisual(this.Child) as HwndSource;
                if (source != null && source.Handle != IntPtr.Zero)
                {
                    _popupHwndSource = source;
                    return;
                }
            }

            // If still null, try to get from popup itself
            var popupSource = PresentationSource.FromVisual(this) as HwndSource;
            if (popupSource != null && popupSource.Handle != IntPtr.Zero)
            {
                _popupHwndSource = popupSource;
            }
        }

        private void SetPopupTopmost()
        {
            if (!this.IsOpen)
                return;

            GetPopupHwndSource();

            if (_popupHwndSource != null && _popupHwndSource.Handle != IntPtr.Zero)
            {
                SetWindowPos(
                    _popupHwndSource.Handle,
                    HWND_TOPMOST,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
    }

    /// <summary>
    /// Placement options for the popup relative to the target window
    /// </summary>
    public enum WindowPlacement
    {
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
