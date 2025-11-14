using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using WindowAttach.Models;
using WindowAttach.Services;
using WINDOW_EX_STYLE = Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;

namespace WindowAttach.ViewModels
{
    /// <summary>
    /// ViewModel for window testing tool
    /// </summary>
    public partial class WindowTestViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<WindowPropertyItem> properties = new();

        [ObservableProperty]
        private string statusMessage = "在\"选择窗口\"按钮上按住鼠标左键，拖动到目标窗口上，然后释放鼠标按钮即可选择窗口";

        [ObservableProperty]
        private bool canRefresh = false;

        private ManagedWindow? _currentManagedWindow;

        [RelayCommand(CanExecute = nameof(CanRefresh))]
        private void RefreshProperties()
        {
            if (_currentManagedWindow != null)
            {
                DisplayWindowProperties();
            }
        }

        /// <summary>
        /// Pick window at screen coordinates
        /// </summary>
        public void PickWindowAtPoint(Point screenPoint)
        {
            try
            {
                // Create POINT structure for WindowFromPoint
                var point = new System.Drawing.Point((int)screenPoint.X, (int)screenPoint.Y);

                // Get window handle at point
                var hwnd = WindowFromPoint(point);
                if (hwnd.Value == IntPtr.Zero || !IsWindow(hwnd))
                {
                    StatusMessage = "未找到有效窗口";
                    return;
                }

                // Dispose previous managed window
                _currentManagedWindow?.Dispose();

                // Create new managed window
                _currentManagedWindow = new ManagedWindow(hwnd.Value);

                // Enable refresh button
                CanRefresh = true;

                // Subscribe to property changes
                SubscribeToPropertyChanges();

                // Display properties
                DisplayWindowProperties();
            }
            catch (Exception ex)
            {
                StatusMessage = $"无法获取窗口信息：{ex.Message}";
                MessageBox.Show($"无法获取窗口信息：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubscribeToPropertyChanges()
        {
            if (_currentManagedWindow == null)
                return;

            _currentManagedWindow.TitleChanged += (s, title) => UpdateProperty("Title", title);
            _currentManagedWindow.IsVisibleChanged += (s, isVisible) => UpdateProperty("IsVisible", isVisible.ToString());
            _currentManagedWindow.IsActiveChanged += (s, isActive) => UpdateProperty("IsActive", isActive.ToString());
            _currentManagedWindow.WindowStateChanged += (s, state) => UpdateProperty("WindowState", state.ToString());
            _currentManagedWindow.TopmostChanged += (s, topmost) => UpdateProperty("Topmost", topmost.ToString());
            _currentManagedWindow.BoundsChanged += (s, bounds) => 
            {
                UpdateProperty("Bounds", bounds.ToString());
                UpdateProperty("Position", $"({bounds.Left}, {bounds.Top})");
                UpdateProperty("Size", $"{bounds.Width} x {bounds.Height}");
            };
            _currentManagedWindow.OpacityChanged += (s, opacity) => UpdateProperty("Opacity", opacity.ToString("F2"));
            _currentManagedWindow.ExStyleChanged += (s, exStyle) => UpdateProperty("ExStyle", $"0x{exStyle:X8}");
            _currentManagedWindow.StyleChanged += (s, style) => UpdateProperty("Style", $"0x{style:X8}");
        }

        private void DisplayWindowProperties()
        {
            if (_currentManagedWindow == null)
                return;

            Properties.Clear();
            StatusMessage = "窗口属性（实时更新）";

            // Display all properties
            AddProperty("Handle", $"0x{_currentManagedWindow.Handle:X}");
            AddProperty("Title", _currentManagedWindow.Title ?? "(No title)");
            AddProperty("IsVisible", _currentManagedWindow.IsVisible.ToString());
            AddProperty("IsActive", _currentManagedWindow.IsActive.ToString());
            AddProperty("WindowState", _currentManagedWindow.WindowState.ToString());
            AddProperty("Topmost", _currentManagedWindow.Topmost.ToString());

            var bounds = _currentManagedWindow.Bounds;
            if (bounds.HasValue)
            {
                AddProperty("Position", $"({bounds.Value.Left}, {bounds.Value.Top})");
                AddProperty("Size", $"{bounds.Value.Width} x {bounds.Value.Height}");
                AddProperty("Bounds", bounds.Value.ToString());
            }
            else
            {
                AddProperty("Bounds", "N/A");
            }

            AddProperty("Opacity", _currentManagedWindow.Opacity.ToString("F2"));
            AddProperty("Owner", $"0x{_currentManagedWindow.Owner:X}");
            AddProperty("Parent", $"0x{_currentManagedWindow.Parent:X}");
            AddProperty("ProcessId", _currentManagedWindow.ProcessId.ToString());
            AddProperty("ThreadId", _currentManagedWindow.ThreadId.ToString());

            // Extended styles
            var exStyleValue = _currentManagedWindow.ExStyle;
            var exStyle = (WINDOW_EX_STYLE)exStyleValue;
            AddProperty("ExStyle", $"0x{exStyleValue:X8}");
            AddProperty("  WS_EX_TOOLWINDOW", exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW).ToString());
            AddProperty("  WS_EX_NOACTIVATE", exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_NOACTIVATE).ToString());
            AddProperty("  WS_EX_TOPMOST", exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOPMOST).ToString());

            // Window style
            var styleValue = _currentManagedWindow.Style;
            AddProperty("Style", $"0x{styleValue:X8}");
        }

        private void AddProperty(string name, string value)
        {
            Properties.Add(new WindowPropertyItem { Name = name, Value = value });
        }

        private void UpdateProperty(string propertyName, string value)
        {
            foreach (var prop in Properties)
            {
                if (prop.Name == propertyName)
                {
                    prop.Value = value;
                    return;
                }
            }
        }

        public void Dispose()
        {
            _currentManagedWindow?.Dispose();
        }
    }

    /// <summary>
    /// Window property item for display
    /// </summary>
    public partial class WindowPropertyItem : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Value { get; set; } = string.Empty;
    }
}
