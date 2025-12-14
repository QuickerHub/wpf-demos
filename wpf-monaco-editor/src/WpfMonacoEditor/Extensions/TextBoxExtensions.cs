using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DependencyPropertyGenerator;

namespace WpfMonacoEditor.Extensions
{
    /// <summary>
    /// Extension behaviors for TextBox control
    /// </summary>
    [AttachedDependencyProperty<bool, TextBox>("SelectAllOnFocus")]
    public static partial class TextBoxExtensions
    {
        /// <summary>
        /// When set to true, automatically selects all text when TextBox gets focus
        /// </summary>
        static partial void OnSelectAllOnFocusChanged(TextBox textBox, bool oldValue, bool newValue)
        {
            if (newValue)
            {
                textBox.GotFocus += TextBox_GotFocus;
                textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
            }
            else
            {
                textBox.GotFocus -= TextBox_GotFocus;
                textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Use Dispatcher to ensure selection happens after focus is fully set
                textBox.Dispatcher.BeginInvoke(new System.Action(() => textBox.SelectAll()), 
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // If TextBox is not focused, focus it first, then select all
                if (!textBox.IsFocused)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                    e.Handled = true; // Prevent default mouse behavior
                }
            }
        }
    }
}

