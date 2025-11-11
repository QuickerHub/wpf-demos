using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickerCodeEditor.View;

namespace QuickerCodeEditor.View.CodeEditor
{
    /// <summary>
    /// A UserControl for displaying and selecting CodeEditorState
    /// </summary>
    public partial class CodeEditorStateControl : UserControl
    {
        /// <summary>
        /// ViewModel for this control
        /// </summary>
        public CodeEditorStateControlViewModel ViewModel { get; set; }

        public CodeEditorStateControl()
        {
            InitializeComponent();
            ViewModel = new CodeEditorStateControlViewModel();
            this.DataContext = this;
            
            this.Loaded += CodeEditorStateControl_Loaded;
        }

        private void CodeEditorStateControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Find the window that contains this control
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Closed += Window_Closed;
            }

            // Subscribe to ListBox SelectionChanged to auto scroll
            if (ListBox != null)
            {
                ListBox.SelectionChanged += ListBox_SelectionChanged;
            }

            // Default select index 0 if available
            if (ViewModel.States != null && ViewModel.States.Count > 0 && ViewModel.SelectedState == null)
            {
                ViewModel.SelectedState = ViewModel.States[0];
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBox != null && ListBox.SelectedItem != null)
            {
                ListBox.ScrollIntoView(ListBox.SelectedItem);
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            // Save current state to selected state when window closes
            if (ViewModel.SelectedState != null && ViewModel.CodeEditorWrapper != null)
            {
                var currentState = ViewModel.CodeEditorWrapper.GetState();
                var selectedState = ViewModel.SelectedState;
                
                // Check if there are any changes
                if (!CodeEditorState.AreContentEqual(selectedState, currentState))
                {
                    selectedState.CopyContentFrom(currentState);
                    selectedState.UpdateTime = DateTime.Now;
                }
            }
            
            // Save states list when window closes
            ViewModel.SaveStatesList();
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as System.Windows.Controls.ListBoxItem;
            if (listBoxItem == null) return;

            var state = listBoxItem.DataContext as CodeEditorState;
            if (state == null) return;

            StartEditing(state);
            e.Handled = true;
        }

        private void StartEditing(CodeEditorState state)
        {
            ViewModel.EditingState = state;
            ViewModel.EditingName = state.Name;
            // Focus will be handled by NameTextBox_IsVisibleChanged or NameTextBox_Loaded
        }

        private void NameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            FocusTextBox(sender as System.Windows.Controls.TextBox);
        }

        private void NameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null && textBox.Visibility == Visibility.Visible && (bool)e.NewValue)
            {
                // Delay focus to ensure TextBox is fully rendered
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    FocusTextBox(textBox);
                }));
            }
        }

        private void FocusTextBox(System.Windows.Controls.TextBox? textBox)
        {
            if (textBox != null && textBox.Visibility == Visibility.Visible)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            if (e.Key == Key.Enter)
            {
                SaveEditing();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelEditing();
                e.Handled = true;
            }
        }

        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveEditing();
        }

        private void SaveEditing()
        {
            if (ViewModel.EditingState != null && !string.IsNullOrWhiteSpace(ViewModel.EditingName))
            {
                ViewModel.EditingState.Name = ViewModel.EditingName.Trim();
                // Save states list after renaming
                ViewModel.SaveStatesList();
            }
            CancelEditing();
        }

        private void CancelEditing()
        {
            ViewModel.EditingState = null;
            ViewModel.EditingName = null;
        }
    }
}
