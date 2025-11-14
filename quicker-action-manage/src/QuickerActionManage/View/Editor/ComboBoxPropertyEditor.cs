using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using ComboBox = System.Windows.Controls.ComboBox;

namespace QuickerActionManage.View.Editor
{
    /// <summary>
    /// Property editor for ComboBox selection (e.g., for process selection)
    /// </summary>
    public class ComboBoxPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var bindingAttr = propertyItem.GetAttribute<PropertyBindingAttribute>();
            var comboBox = new ComboBox
            {
                IsEnabled = !propertyItem.IsReadOnly,
                ItemsSource = bindingAttr?.GetItemsSource(),
                MaxDropDownHeight = 200 // Set max height to enable scrolling when items exceed this height
            };
            // Set ScrollViewer attached properties after creation
            System.Windows.Controls.ScrollViewer.SetCanContentScroll(comboBox, true);
            System.Windows.Controls.ScrollViewer.SetVerticalScrollBarVisibility(comboBox, ScrollBarVisibility.Auto);
            return comboBox;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return ComboBox.SelectedValueProperty;
        }
    }
}

