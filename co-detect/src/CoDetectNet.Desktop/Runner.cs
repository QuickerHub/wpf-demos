using System.Windows;

namespace CoDetectNet.Desktop
{
    public static class Runner
    {
        public static void Run(string input = "")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = new MainWindow();
                win.Show();
            });
        }
    }
}

