using System.Linq;
using System.Windows;
using Quicker.Public;
using Quicker.Common;
using Quicker.Domain;
using Quicker.Public.Interfaces;

namespace QuickerTools
{
    /// <summary>
    /// Runner for Quicker interface - provides entry points for Quicker actions
    /// </summary>
    public static class Runner
    {
        private static MainWindow? _mainWindow;

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        public static void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // If window exists and is not closed, activate it
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    return;
                }

                // Create new window instance
                _mainWindow = new MainWindow();

                // Remove reference when window is closed
                _mainWindow.Closed += (s, e) =>
                {
                    _mainWindow = null;
                };

                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
            });
        }

        /// <summary>
        /// Show Quicker tray menu (Quicker interface)
        /// </summary>
        public static void ShowQuickerTrayMenu()
        {
            QuickerUtils.ShowTrayMenu();
        }

        /// <summary>
        /// Get action data from IActionContext
        /// Returns the first non-empty data field (Data, ContextMenuData, etc.)
        /// </summary>
        /// <param name="context">Action context</param>
        /// <returns>Action data string, or null if not available</returns>
        public static string? GetActionData(IActionContext context)
        {
            if (context == null)
            {
                return null;
            }

            try
            {
                // Get ActionId from root context
                var rootContext = context.GetRootContext();
                var actionId = rootContext?.ActionId;

                if (string.IsNullOrEmpty(actionId))
                {
                    return null;
                }

                // Get ActionItem from DataService
                var (action, _) = AppState.DataService.GetActionById(actionId);
                if (action == null)
                {
                    return null;
                }

                // Check multiple data fields and return the first one with content
                // Priority: Data > Data2 > Data3
                return new[] { action.Data, action.Data2, action.Data3 }
                    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            }
            catch
            {
                return null;
            }
        }
    }
}

