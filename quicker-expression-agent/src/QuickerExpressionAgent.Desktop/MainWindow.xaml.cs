using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = this; // Set DataContext to this, not ViewModel (following WPF coding standards)
            
            // Subscribe to chat messages collection changes for auto-scroll
            ViewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            ViewModel.ChatScrollToBottomRequested += ChatScrollToBottomRequested;
            
            // Bind AvalonEdit TextEditor to ViewModel.Expression
            if (ParsedExpressionEditor != null)
            {
                // Two-way binding: ViewModel -> Editor
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                UpdateEditorText();
                
                // Two-way binding: Editor -> ViewModel
                ParsedExpressionEditor.TextChanged += (s, e) =>
                {
                    if (ViewModel.Expression != ParsedExpressionEditor.Text)
                    {
                        ViewModel.Expression = ParsedExpressionEditor.Text;
                    }
                };
            }
        }
        
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Expression))
            {
                UpdateEditorText();
            }
        }
        
        private void UpdateEditorText()
        {
            if (ParsedExpressionEditor != null && ViewModel != null)
            {
                if (ParsedExpressionEditor.Text != ViewModel.Expression)
                {
                    ParsedExpressionEditor.Text = ViewModel.Expression ?? string.Empty;
                }
            }
        }

        private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Scroll to bottom when new messages are added
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    ScrollChatToBottom();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ChatScrollToBottomRequested(object? sender, System.EventArgs e)
        {
            ScrollChatToBottom();
        }

        private void ScrollChatToBottom()
        {
            if (ChatListBox != null && ChatListBox.Items.Count > 0)
            {
                ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
            }
        }

        private void ChatInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter key without Shift: trigger send command
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;  // Prevent default newline behavior
                if (ViewModel.GenerateCommand?.CanExecute(null) == true)
                {
                    ViewModel.GenerateCommand.Execute(null);
                }
            }
            // Shift+Enter: allow default behavior (new line) - don't handle, let it bubble
        }

    }
}
