using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// SortDirectionRadioButtonControl.xaml 的交互逻辑
    /// </summary>
    public partial class SortDirectionRadioButtonControl : UserControl
    {
        public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.Register(
            nameof(SortDirection),
            typeof(ListSortDirection),
            typeof(SortDirectionRadioButtonControl),
            new FrameworkPropertyMetadata(
                ListSortDirection.Ascending,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSortDirectionChanged));

        public ListSortDirection SortDirection
        {
            get => (ListSortDirection)GetValue(SortDirectionProperty);
            set => SetValue(SortDirectionProperty, value);
        }

        private static void OnSortDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SortDirectionRadioButtonControl control)
            {
                control.UpdateRadioButtons();
            }
        }

        public SortDirectionRadioButtonControl()
        {
            InitializeComponent();
            
            // 设置 GroupName，确保只能选一个
            var groupName = $"SortDirection_{Guid.NewGuid()}";
            AscendingRadio.GroupName = groupName;
            DescendingRadio.GroupName = groupName;

            // 绑定事件
            AscendingRadio.Checked += (s, e) =>
            {
                if (AscendingRadio.IsChecked == true)
                {
                    SortDirection = ListSortDirection.Ascending;
                }
            };

            DescendingRadio.Checked += (s, e) =>
            {
                if (DescendingRadio.IsChecked == true)
                {
                    SortDirection = ListSortDirection.Descending;
                }
            };
            
            // 在加载后确保对齐，防止 PropertyItem 的布局影响
            Loaded += (s, e) =>
            {
                // 强制重新布局
                AscendingRadio.UpdateLayout();
                DescendingRadio.UpdateLayout();
                
                // 确保对齐
                AscendingRadio.VerticalAlignment = VerticalAlignment.Center;
                DescendingRadio.VerticalAlignment = VerticalAlignment.Center;
            };
        }

        private void UpdateRadioButtons()
        {
            if (AscendingRadio != null && DescendingRadio != null)
            {
                AscendingRadio.IsChecked = SortDirection == ListSortDirection.Ascending;
                DescendingRadio.IsChecked = SortDirection == ListSortDirection.Descending;
            }
        }
    }
}

