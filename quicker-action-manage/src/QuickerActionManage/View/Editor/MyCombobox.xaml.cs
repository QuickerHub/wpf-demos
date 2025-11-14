using System;
using System.Windows.Controls;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.View.Editor
{
    public static class ItemsControlExt
    {
        public static void CreateItemsSourceByEnum(this ItemsControl itemsControl, Type? type)
        {
            if (itemsControl.ItemsSource == null && type != null)
            {
                itemsControl.ItemsSource = EnumExtensions.GetValuesOfBrowsable(type);
            }
        }
    }

    /// <summary>
    /// MyCombobox.xaml 的交互逻辑
    /// </summary>
    public partial class MyCombobox : ComboBox
    {
        public MyCombobox()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                this.CreateItemsSourceByEnum(SelectedValue?.GetType());
            };
        }
    }
}

