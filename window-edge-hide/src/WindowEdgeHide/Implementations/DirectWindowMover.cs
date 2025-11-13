using System;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowEdgeHide.Interfaces;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide.Implementations
{
    /// <summary>
    /// Direct window mover - moves window immediately without animation
    /// </summary>
    public class DirectWindowMover : IWindowMover
    {
        /// <summary>
        /// Move window to specified position immediately
        /// </summary>
        public void MoveWindow(IntPtr windowHandle, int x, int y, int width, int height)
        {
            WindowHelper.SetWindowPos(windowHandle, x, y, width, height,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }
    }
}

