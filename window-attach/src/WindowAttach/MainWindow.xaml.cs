using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowAttach.Models;
using WindowAttach.ViewModels;

namespace WindowAttach
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowTestViewModel? _viewModel;
        private Window? _testWindow;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = DataContext as WindowTestViewModel;
            
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
                        placement: WindowAttach.Models.WindowPlacement.RightTop,
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
            
            _viewModel?.Dispose();
        }

        private void PickWindowButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start drag operation
            var data = new DataObject("WindowPicker", "pick");
            DragDrop.DoDragDrop(PickWindowButton, data, DragDropEffects.None);
            
            // After drag ends, get current cursor position and pick window
            if (GetCursorPos(out System.Drawing.Point screenPoint))
            {
                var screenPos = new Point(screenPoint.X, screenPoint.Y);
                _viewModel?.PickWindowAtPoint(screenPos);
            }
        }

        private void PickWindowButton_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // Change cursor to crosshair during drag
            Mouse.SetCursor(Cursors.Cross);
            e.Handled = true;
        }

        private void TestWindowAttach_Click(object sender, RoutedEventArgs e)
        {
            Runner.ShowWindowList();
        }
    }
}
