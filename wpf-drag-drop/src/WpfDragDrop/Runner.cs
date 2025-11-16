using System.Windows;

namespace WpfDragDrop
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class Runner
    {
        /// <summary>
        /// Show the main window
        /// </summary>
        public static void ShowMainWindow()
        {
            var window = new MainWindow();
            window.Show();
        }
    }
}

