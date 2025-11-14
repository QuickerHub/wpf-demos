using System.Windows;
using System.Linq;
using HandyControl.Controls;

namespace QuickerActionManage.View.Editor
{
    public class NumEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var attr = propertyItem.PropertyDescriptor.Attributes.OfType<NumEditorPropertyAttribute>().FirstOrDefault();
            var control = new NumericUpDown
            {
                IsReadOnly = propertyItem.IsReadOnly
            };
            if (attr != null)
            {
                control.Minimum = attr.Minimum;
                control.Maximum = attr.Maximum;
                control.Increment = attr.Increment;
            }
            return control;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return NumericUpDown.ValueProperty;
        }
    }
}

