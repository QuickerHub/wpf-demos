using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WindowEdgeHide.Utils;

namespace WindowEdgeHide
{
    /// <summary>
    /// Management window for edge hiding services
    /// </summary>
    public partial class ManagementWindow : Window
    {
        private ObservableCollection<WindowInfo> _windows = new ObservableCollection<WindowInfo>();

        public ManagementWindow()
        {
            InitializeComponent();
            WindowsDataGrid.ItemsSource = _windows;
            RefreshWindowList();
        }

        private void RefreshWindowList()
        {
            _windows.Clear();
            
            var registeredWindows = Runner.GetRegisteredWindows();
            foreach (var handle in registeredWindows)
            {
                if (handle != IntPtr.Zero)
                {
                    string title = WindowHelper.GetWindowTitle(handle);
                    string className = WindowHelper.GetWindowClassName(handle);
                    
                    _windows.Add(new WindowInfo
                    {
                        Handle = handle,
                        Title = string.IsNullOrEmpty(title) ? "(无标题)" : title,
                        ClassName = string.IsNullOrEmpty(className) ? "(未知)" : className
                    });
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindowList();
        }

        private void UnregisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is IntPtr handle)
            {
                bool success = Runner.UnregisterEdgeHide(handle);
                if (success)
                {
                    MessageBox.Show("已取消贴边隐藏", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshWindowList();
                }
                else
                {
                    MessageBox.Show("取消贴边隐藏失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UnregisterAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要取消所有窗口的贴边隐藏吗？", "确认", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                int count = Runner.UnregisterAll();
                MessageBox.Show($"已取消 {count} 个窗口的贴边隐藏", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshWindowList();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
        }
    }
}

