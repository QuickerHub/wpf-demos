using System;
using System.Windows.Controls;
using Quicker.Common;
using Quicker.View;
using Quicker.Utilities;
using Quicker.Domain;

namespace QuickerTools
{
    /// <summary>
    /// Quicker utility methods
    /// </summary>
    public static class QuickerUtils
    {
        /// <summary>
        /// Get main window
        /// </summary>
        public static PopupWindow GetMainWindow()
        {
            return WindowHelper.GetWindow(AppState.MainWinHandle) as PopupWindow ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Quicker tray menu
        /// </summary>
        public static void ShowTrayMenu()
        {
            var win = GetMainWindow();
            var menu = (ContextMenu)win.FindResource("NotifierContextMenu");
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            menu.IsOpen = true;
        }
    }
}

