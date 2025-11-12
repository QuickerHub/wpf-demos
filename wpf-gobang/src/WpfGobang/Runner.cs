using System.Windows;

namespace WpfGobang
{
    /// <summary>
    /// Runner for showing main window (singleton pattern)
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
    }
}

