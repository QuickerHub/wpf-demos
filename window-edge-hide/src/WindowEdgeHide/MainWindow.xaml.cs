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
            UseAnimationCheckBox.IsChecked = true; // Default to use animation
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

                // Get animation setting
                bool useAnimation = UseAnimationCheckBox.IsChecked == true;

                // Get edge direction (default to Nearest)
                EdgeDirection edgeDirection = EdgeDirection.Nearest;
                if (EdgeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
                {
                    string? selectedValue = selectedItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedValue))
                    {
                        edgeDirection = selectedValue switch
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
                var result = Runner.EnableEdgeHide(hwnd, edgeDirection, visibleArea, useAnimation);
                
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

