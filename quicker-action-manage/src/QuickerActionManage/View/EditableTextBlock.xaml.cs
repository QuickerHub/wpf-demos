using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DependencyPropertyGenerator;

namespace QuickerActionManage.View
{
    /// <summary>
    /// EditableTextBlock.xaml 的交互逻辑
    /// </summary>
    [DependencyProperty<string>("Text")]
    [DependencyProperty<bool>("IsEditing")]
    [DependencyProperty<bool>("CanEdit", DefaultValue = true)]
    public partial class EditableTextBlock : UserControl
    {
        private string? _originalText;

        public EditableTextBlock()
        {
            InitializeComponent();
            // Set default values for inherited properties
            FontSize = 13.0;
            FontWeight = FontWeights.Normal;
        }

        partial void OnIsEditingChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                // Check if editing is allowed
                if (!CanEdit)
                {
                    // Prevent editing if CanEdit is false
                    IsEditing = false;
                    return;
                }
                
                // Save original text when entering edit mode
                _originalText = Text;
                // Delay focus to ensure TextBox is fully rendered
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    FocusEditTextBox();
                }));
            }
            else
            {
                // Clear saved text when exiting edit mode
                _originalText = null;
            }
        }

        private void FocusEditTextBox()
        {
            if (EditTextBox != null && EditTextBox.Visibility == Visibility.Visible)
            {
                EditTextBox.Focus();
                EditTextBox.SelectAll();
            }
        }

        private void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsEditing)
            {
                FocusEditTextBox();
            }
        }

        private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsEditing && (bool)e.NewValue)
            {
                // Delay focus to ensure TextBox is fully rendered
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    FocusEditTextBox();
                }));
            }
        }

        private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (e.Key == Key.Enter)
            {
                IsEditing = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel editing by reverting to original text
                if (_originalText != null)
                {
                    Text = _originalText;
                }
                IsEditing = false;
                e.Handled = true;
            }
        }

        private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsEditing = false;
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to start editing (if CanEdit allows)
            if (CanEdit)
            {
                IsEditing = true;
                e.Handled = true;
            }
        }
    }
}

