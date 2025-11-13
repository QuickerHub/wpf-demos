using System;
using System.Windows;
using System.Windows.Interop;
using WindowEdgeHide.Models;
using WindowEdgeHide.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace WindowEdgeHide.Services
{
    /// <summary>
    /// Debug window to visualize trigger area
    /// </summary>
    public partial class TriggerAreaDebugWindow : Window
    {
        private IntPtr _hwnd = IntPtr.Zero;

        public TriggerAreaDebugWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.Manual;
            // Set initial size to 0 to avoid window flashing
            Width = 0;
            Height = 0;
        }

        /// <summary>
        /// Update window position and size to match trigger area exactly
        /// Uses Win32 API to ensure pixel-perfect alignment
        /// </summary>
        public void UpdateTriggerArea(WindowRect triggerArea)
        {
            if (_hwnd == IntPtr.Zero)
                return;

            var hwnd = new HWND(_hwnd);
            
            // Use SetWindowPos to set position and size exactly matching triggerArea
            // SWP_NOZORDER | SWP_NOACTIVATE to avoid focus issues
            SetWindowPos(
                hwnd,
                HWND.Null,
                triggerArea.Left,
                triggerArea.Top,
                Math.Max(1, triggerArea.Width),
                Math.Max(1, triggerArea.Height),
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            );
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Get window handle
            _hwnd = new WindowInteropHelper(this).Handle;
            if (_hwnd != IntPtr.Zero)
            {
                // Set WS_EX_NOACTIVATE to prevent window from getting focus
                WindowHelper.SetWindowExStyle(_hwnd, WINDOW_EX_STYLE.WS_EX_NOACTIVATE, true);
                // Set WS_EX_TRANSPARENT to make window mouse-transparent (mouse clicks pass through)
                WindowHelper.SetWindowExStyle(_hwnd, WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
            }
        }
    }
}

