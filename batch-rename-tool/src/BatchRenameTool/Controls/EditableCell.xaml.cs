using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls
{
    /// <summary>
    /// EditableCell.xaml 的交互逻辑
    /// </summary>
    [DependencyProperty<string>("Text", DefaultBindingMode = DefaultBindingMode.TwoWay, DefaultUpdateSourceTrigger = SourceTrigger.PropertyChanged)]
    [DependencyProperty<bool>("IsEditing")]
    public partial class EditableCell : UserControl
    {
        /// <summary>
        /// Event raised when Enter key is pressed and editing should move to next cell
        /// </summary>
        public event EventHandler? MoveToNextCell;
        public EditableCell()
        {
            InitializeComponent();
            // Initialize TextBlock with current Text value
            Loaded += (s, e) =>
            {
                if (DisplayTextBlock != null)
                {
                    DisplayTextBlock.Text = Text ?? "";
                }
            };
        }

        partial void OnTextChanged(string? value)
        {
            // Update TextBlock display when Text changes
            // Don't use binding to avoid conflicts with external bindings
            if (DisplayTextBlock != null)
            {
                DisplayTextBlock.Text = value ?? "";
            }
        }

        partial void OnIsEditingChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                // Set TextBox text from current Text value when entering edit mode
                if (EditTextBox != null)
                {
                    EditTextBox.Text = Text;
                }
                // Delay focus to ensure TextBox is fully rendered
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    FocusEditTextBox();
                }));
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
                // Update Text property with TextBox value when editing completes
                Text = textBox.Text;
                IsEditing = false;
                
                // Raise event to move to next cell
                MoveToNextCell?.Invoke(this, EventArgs.Empty);
                
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Cancel editing: ignore TextBox content, don't update Text property
                // Just exit edit mode, Text property remains unchanged
                IsEditing = false;
                e.Handled = true;
            }
        }

        private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Update Text property with TextBox value when editing completes
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                Text = textBox.Text;
            }
            IsEditing = false;
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to start editing
            IsEditing = true;
            e.Handled = true;
        }
    }
}
