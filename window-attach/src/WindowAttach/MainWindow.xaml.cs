using System;
using System.Windows;
using System.Windows.Interop;
using WindowAttach.Models;
using WindowAttach.Services;
using WindowAttach;

namespace WindowAttach
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Window? _testWindow;

        public MainWindow()
        {
            InitializeComponent();
            
            // Setup window attachment after window handle is available
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Use dispatcher to ensure window is fully rendered
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SetupWindowAttachment();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void SetupWindowAttachment()
        {
            try
            {
                // Create a test window (window2)
                _testWindow = new Window
                {
                    Title = "吸附窗口（测试）",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Background = System.Windows.Media.Brushes.LightBlue
                };

                // Add some content to the test window
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = "这是吸附窗口\n吸附到主窗口的右侧顶部\n已启用自动调整位置功能",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                _testWindow.Content = textBlock;

                // Setup SourceInitialized event to get handle when available
                _testWindow.SourceInitialized += (s, e) =>
                {
                    // Get window handles
                    var mainWindowHandle = new WindowInteropHelper(this).Handle;
                    var testWindowHandle = new WindowInteropHelper(_testWindow).Handle;

                    if (mainWindowHandle == IntPtr.Zero || testWindowHandle == IntPtr.Zero)
                    {
                        MessageBox.Show("无法获取窗口句柄", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Register window attachment using Runner (so it appears in management window)
                    // Enable autoAdjustToScreen to test automatic position adjustment
                    Runner.Register(
                        window1Handle: mainWindowHandle,
                        window2Handle: testWindowHandle,
                        placement: WindowPlacement.RightTop,
                        offsetX: 10,  // 10 pixels offset from the right edge
                        offsetY: 0,   // 0 pixels offset from the top
                        restrictToSameScreen: false,
                        autoAdjustToScreen: true  // Enable automatic position adjustment to maximize visible area
                    );
                };

                // Show the test window
                _testWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置窗口吸附时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Cleanup
            if (_testWindow != null)
            {
                var mainWindowHandle = new WindowInteropHelper(this).Handle;
                var testWindowHandle = new WindowInteropHelper(_testWindow).Handle;
                
                // Unregister window attachment
                if (mainWindowHandle != IntPtr.Zero && testWindowHandle != IntPtr.Zero)
                {
                    Runner.Unregister(mainWindowHandle, testWindowHandle);
                }
                
                _testWindow.Close();
            }
        }

        private void OpenManagementWindow_Click(object sender, RoutedEventArgs e)
        {
            Runner.ShowWindowList();
        }
    }
}

