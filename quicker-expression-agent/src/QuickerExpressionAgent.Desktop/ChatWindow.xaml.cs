using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindowViewModel ViewModel { get; }

        public ChatWindow(ChatWindowViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = this; // Set DataContext to this, not ViewModel (following WPF coding standards)
            
            // Subscribe to chat messages collection changes for auto-scroll
            ViewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            ViewModel.ChatScrollToBottomRequested += ChatScrollToBottomRequested;
        }

        private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Scroll to bottom when new messages are added
            // Use BeginInvoke with Loaded priority to avoid ItemContainerGenerator inconsistency
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
            // Delay scroll to avoid ItemContainerGenerator inconsistency
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                ScrollChatToBottom();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ScrollChatToBottom()
        {
            if (ChatListBox != null && ChatListBox.Items.Count > 0)
            {
                try
                {
                    ChatListBox.ScrollIntoView(ChatListBox.Items[ChatListBox.Items.Count - 1]);
                }
                catch
                {
                    // Ignore scroll errors during collection updates
                }
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


        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Dispose();
            base.OnClosed(e);
        }
    }
}

