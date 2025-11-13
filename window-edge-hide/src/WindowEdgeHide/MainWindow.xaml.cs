using System;
using System.Windows;
using System.Windows.Interop;
using WindowEdgeHide.Models;

namespace WindowEdgeHide
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set default values
            AnimationTypeComboBox.SelectedIndex = 2; // Default to EaseInOut
            VisibleAreaTextBox.Text = "-5"; // Default visible area to -5
            
            UpdateStatus();
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get window handle
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    StatusTextBlock.Text = "Status: Error - Window handle is zero";
                    return;
                }

                // Get visible area thickness (parse from text box or use default)
                string visibleAreaText = VisibleAreaTextBox?.Text ?? "5";
                IntThickness visibleArea;
                try
                {
                    visibleArea = IntThickness.Parse(visibleAreaText);
                }
                catch
                {
                    visibleArea = new IntThickness(5); // Default to 5 if parsing fails
                }

                // Get animation type from ComboBox
                AnimationType animationType = AnimationType.None;
                if (AnimationTypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem animationItem)
                {
                    string? animationValue = animationItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(animationValue))
                    {
                        animationType = animationValue switch
                        {
                            "None" => AnimationType.None,
                            "Linear" => AnimationType.Linear,
                            "EaseInOut" => AnimationType.EaseInOut,
                            _ => AnimationType.None
                        };
                    }
                }

                // Get edge direction (default to Nearest)
                EdgeDirection edgeDirection = EdgeDirection.Nearest;
                if (EdgeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem edgeItem)
                {
                    string? edgeValue = edgeItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(edgeValue))
                    {
                        edgeDirection = edgeValue switch
                        {
                            "Left" => EdgeDirection.Left,
                            "Top" => EdgeDirection.Top,
                            "Right" => EdgeDirection.Right,
                            "Bottom" => EdgeDirection.Bottom,
                            "Nearest" => EdgeDirection.Nearest,
                            _ => EdgeDirection.Nearest
                        };
                    }
                }

                // Enable edge hide
                var result = Runner.EnableEdgeHide(hwnd, edgeDirection, visibleArea, animationType);
                
                if (result.Success)
                {
                    UpdateStatus();
                    StatusTextBlock.Text = $"Status: {result.Message} - Visible Area: {visibleArea}";
                }
                else
                {
                    StatusTextBlock.Text = $"Status: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Status: Error - {ex.Message}";
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    StatusTextBlock.Text = "Status: Error - Window handle is zero";
                    return;
                }

                // Unregister edge hiding
                bool success = Runner.UnregisterEdgeHide(hwnd);
                
                UpdateStatus();
                StatusTextBlock.Text = success ? "Status: 贴边隐藏已取消" : "Status: 取消贴边隐藏失败（未启用）";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Status: Error - {ex.Message}";
            }
        }

        private void ManageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new ManagementWindow();
                window.Show();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Status: Error - {ex.Message}";
            }
        }

        private void StopAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要取消所有窗口的贴边隐藏吗？", "确认", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    int count = Runner.UnregisterAll();
                    UpdateStatus();
                    StatusTextBlock.Text = $"Status: 已取消 {count} 个窗口的贴边隐藏";
                    MessageBox.Show($"已取消 {count} 个窗口的贴边隐藏", "成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Status: Error - {ex.Message}";
            }
        }

        private void UpdateStatus()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero && Runner.IsEnabled(hwnd))
            {
                StatusTextBlock.Text = "Status: Enabled";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                StatusTextBlock.Text = "Status: Not enabled";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
    }
}

