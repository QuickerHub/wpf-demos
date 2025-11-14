using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// MyCheckComboBox.xaml 的交互逻辑
    /// 使用 SelectedEnumValue 绑定 Enum
    /// </summary>
    public partial class MyCheckComboBox : CheckComboBox
    {
        public MyCheckComboBox()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (SelectedEnumValue != null && SelectedEnumValue is Enum ev)
                {
                    _enumType = ev.GetType();
                    if (ItemsSource == null)
                    {
                        ItemsSource = ev.GetValuesByBrowsable();
                    }

                    var values = GetFlagValues(ev, ItemsSource);
                    SetSelectedItems(values);
                }
            };
            SelectionChanged += MyCheckComboBox_SelectionChanged;
        }
        private Type? _enumType;

        public static readonly DependencyProperty SelectedEnumValueProperty
            = DependencyProperty.Register(nameof(SelectedEnumValue), typeof(object), typeof(MyCheckComboBox), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public object SelectedEnumValue
        {
            get { return GetValue(SelectedEnumValueProperty); }
            set { SetValue(SelectedEnumValueProperty, value); }
        }

        private void MyCheckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (_enumType == null) return;
            
            int v = 0;
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                v = SelectedItems.Cast<int>().Aggregate((x, y) => x | y);
            }
            SelectedEnumValue = Enum.ToObject(_enumType, v);
        }

        private System.Collections.IList GetFlagValues(Enum enumValue, System.Collections.IEnumerable? itemsSource)
        {
            var result = new System.Collections.ArrayList();
            if (itemsSource == null) return result;
            
            var intValue = Convert.ToInt32(enumValue);
            foreach (var item in itemsSource)
            {
                if (item is Enum ev)
                {
                    var itemValue = Convert.ToInt32(ev);
                    if ((intValue & itemValue) == itemValue && itemValue != 0)
                    {
                        result.Add(item);
                    }
                }
            }
            return result;
        }
    }
}

