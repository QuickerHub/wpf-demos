using System;
using System.Threading.Tasks;
using System.Windows;
using QuickerStatisticsInfo.View;

namespace QuickerStatisticsInfo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            string userPath = UserPathTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userPath))
            {
                MessageBox.Show("请输入用户路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create and show statistics window immediately
            var statisticsWindow = new StatisticsWindow();
            statisticsWindow.Initialize(userPath);
            statisticsWindow.WindowState = WindowState.Normal;
            statisticsWindow.Show();
            statisticsWindow.Activate();
            
            // Force window to render before starting collection
            statisticsWindow.UpdateLayout();

            // Start collecting statistics asynchronously
            statisticsWindow.StartCollecting(userPath);
        }
    }
}

