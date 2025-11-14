using System;
using System.Windows;
using System.Reflection;
using System.Windows.Controls.Primitives;
using HandyControl.Controls;
using HandyControl.Data;

namespace QuickerActionManage.View.Editor
{
    public class EnumEditor : PropertyEditorBase
    {
        private bool _hasflags;
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            _hasflags = propertyItem.PropertyType.GetCustomAttribute<FlagsAttribute>() != null;
            Selector selector;
            if (_hasflags)
            {
                selector = new MyCheckComboBox();
            }
            else
            {
                selector = new MyCombobox();
            }
            selector.IsEnabled = !propertyItem.IsReadOnly;
            selector.ItemsSource = propertyItem.GetAttribute<PropertyBindingAttribute>()?.GetItemsSource();
            return selector;
        }
        /// <summary>
        /// 绑定到的属性
        /// </summary>
        public override DependencyProperty GetDependencyProperty()
        {
            return _hasflags ? MyCheckComboBox.SelectedEnumValueProperty : Selector.SelectedValueProperty;
        }
    }

    public class ListEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var selector = new CheckComboBox
            {
                IsEnabled = !propertyItem.IsReadOnly,
                ItemsSource = propertyItem.GetAttribute<PropertyBindingAttribute>()?.GetItemsSource()
            };
            return selector;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return CheckComboBox.SelectedItemProperty;
        }
    }
}

