using System.Windows;

namespace ActionPathConvert
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
                if (_mainWindow != null)
                {
                    // Check if window is still valid (not closed)
                    if (_mainWindow.IsLoaded)
                    {
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.Show();
                        _mainWindow.Activate();
                        return;
                    }
                    else
                    {
                        // Window was closed, clear reference
                        _mainWindow = null;
                    }
                }

                // Create new window instance (singleton)
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
    }
}

