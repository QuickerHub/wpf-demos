using System.Windows;
using System.Linq;
using System.Windows.Controls;
using HandyControl.Controls;
using TextBox = System.Windows.Controls.TextBox;

namespace QuickerActionManage.View.Editor
{
    public class TextPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            var textbox = new TextBox
            {
                IsReadOnly = propertyItem.IsReadOnly,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            if (textbox.IsReadOnly)
            {
                textbox.PreviewMouseDoubleClick += (s, e) =>
                {
                    e.Handled = true;
                    Clipboard.SetText(textbox.Text);
                };
            }
            var attr = propertyItem.GetAttributes<TextPropertyEditorAttribute>().FirstOrDefault() ?? TextPropertyEditorAttribute.Default;
            attr.SetToTextBox(textbox);
            return textbox;
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return TextBox.TextProperty;
        }
    }
}

