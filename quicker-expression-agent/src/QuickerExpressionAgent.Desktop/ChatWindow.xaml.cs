using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.Extensions;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.ViewModels;
using WindowAttach.Extensions;
using WindowAttach.Models;
using WindowAttach.Services;
using WindowAttach.Utils;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindowViewModel ViewModel { get; }
        private readonly WindowAttachService _windowAttachService;
        private readonly ChatWindowService _chatWindowService;
        private IntPtr _chatWindowHandle = IntPtr.Zero;
        private IntPtr _codeEditorHandle = IntPtr.Zero;

        public ChatWindow(
            ChatWindowViewModel vm,
            WindowAttachService windowAttachService,
            ChatWindowService chatWindowService)
        {
            InitializeComponent();
            ViewModel = vm;
            _windowAttachService = windowAttachService ?? throw new ArgumentNullException(nameof(windowAttachService));
            _chatWindowService = chatWindowService ?? throw new ArgumentNullException(nameof(chatWindowService));
            DataContext = this; // Set DataContext to this, not ViewModel (following WPF coding standards)

            // Subscribe to chat messages collection changes for auto-scroll
            ViewModel.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            ViewModel.ChatScrollToBottomRequested += ChatScrollToBottomRequested;

            // Subscribe to SourceInitialized to get window handle
            SourceInitialized += ChatWindow_SourceInitialized;

            // Subscribe to handler ID changes to attach to CodeEditor window
            ViewModel.CodeEditorHandlerIdChanged += ViewModel_CodeEditorHandlerIdChanged;
        }

        /// <summary>
        /// Command to close the window (bound to ESC key)
        /// </summary>
        public ICommand CloseCommand => new RelayCommand(() => Close());

        private void ChatWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Get ChatWindow handle
            _chatWindowHandle = new WindowInteropHelper(this).Handle;

            // Try to attach if CodeEditor window handle is already available
            TryAttachToCodeEditor();
        }

        private async void ViewModel_CodeEditorHandlerIdChanged(object? sender, string handlerId)
        {
            // Ensure ChatWindow handle is ready
            if (_chatWindowHandle == IntPtr.Zero)
            {
                _chatWindowHandle = new WindowInteropHelper(this).Handle;
            }

            // Wait for CodeEditor window to be fully initialized
            await WaitForCodeEditorWindowAsync(handlerId);

            // Try to attach to CodeEditor window
            await TryAttachToCodeEditorAsync(handlerId);
        }

        private async Task WaitForCodeEditorWindowAsync(string handlerId)
        {
            const int maxRetries = 50; // Maximum 5 seconds (50 * 100ms)
            const int delayMs = 100;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var windowHandleValue = await ViewModel.GetCodeEditorWindowHandleAsync(handlerId);
                    if (windowHandleValue != 0)
                    {
                        var codeEditorHandle = new IntPtr(windowHandleValue);
                        if (WindowHelper.IsWindow(codeEditorHandle))
                        {
                            return; // Window is ready
                        }
                    }
                }
                catch
                {
                    // Ignore errors during polling
                }

                await Task.Delay(delayMs);
            }
        }

        private async System.Threading.Tasks.Task TryAttachToCodeEditorAsync(string handlerId)
        {
            if (_chatWindowHandle == IntPtr.Zero)
            {
                return;
            }

            // Get CodeEditor window handle from handlerId
            var windowHandleValue = await ViewModel.GetCodeEditorWindowHandleAsync(handlerId);
            if (windowHandleValue == 0)
            {
                return;
            }

            // Register ChatWindow for this CodeEditorWindow in ChatWindowService
            // This ensures that when CodeEditorWindow is detected, HasChatWindow will return true
            _chatWindowService.RegisterChatWindowForCodeEditor(this, windowHandleValue);

            var codeEditorHandle = new IntPtr(windowHandleValue);
            AttachToWindowInternal(codeEditorHandle, bringToForeground: true);
        }

        private void TryAttachToCodeEditor()
        {
            if (ViewModel.CodeEditorHandlerId != null)
            {
                TryAttachToCodeEditorAsync(ViewModel.CodeEditorHandlerId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Attach ChatWindow to the specified window handle
        /// </summary>
        /// <param name="targetWindowHandle">Target window handle to attach to</param>
        /// <param name="bringToForeground">Whether to bring target window to foreground (default: true)</param>
        public void AttachToWindow(IntPtr targetWindowHandle, bool bringToForeground = true)
        {
            if (_chatWindowHandle == IntPtr.Zero)
            {
                // Ensure handle is available
                _chatWindowHandle = new WindowInteropHelper(this).Handle;
                if (_chatWindowHandle == IntPtr.Zero)
                {
                    return;
                }
            }

            AttachToWindowInternal(targetWindowHandle, bringToForeground);
        }

        /// <summary>
        /// Internal method to attach ChatWindow to a target window
        /// </summary>
        /// <param name="targetWindowHandle">Target window handle to attach to</param>
        /// <param name="bringToForeground">Whether to bring target window to foreground</param>
        private void AttachToWindowInternal(IntPtr targetWindowHandle, bool bringToForeground)
        {
            // Check if this is the first attachment
            bool isFirstAttachment = _codeEditorHandle == IntPtr.Zero;

            // Unregister previous attachment if exists
            if (_codeEditorHandle != IntPtr.Zero)
            {
                _windowAttachService.Unregister(_codeEditorHandle, _chatWindowHandle);
            }

            // Store current target window handle
            _codeEditorHandle = targetWindowHandle;

            // Attach ChatWindow to target window
            // Add callback to stop generation and close window when target window is closed
            _windowAttachService.Register(
                targetWindowHandle,  // window1: Target window (window to follow)
                _chatWindowHandle,   // window2: ChatWindow (window that follows)
                placement: WindowPlacement.RightTop,
                offsetX: 0,
                offsetY: 0,
                restrictToSameScreen: true,
                autoOptimizePosition: false,
                callbackAction: () =>
                {
                    // Stop generation and close window when target window is closed
                    Dispatcher.Invoke(() =>
                    {
                        ViewModel.StopGeneration();
                        Close();
                    });
                }
            );

            // Bring target window to foreground on first attachment
            if (bringToForeground && isFirstAttachment)
            {
                WindowHelper.BringWindowToForeground(targetWindowHandle);
            }
        }

        private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Scroll to bottom when new messages are added
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                Dispatcher.InvokeOnRender(() => ChatListBox?.ScrollToBottom());
            }
        }

        private void ChatScrollToBottomRequested(object? sender, System.EventArgs e)
        {
            // Scroll to bottom when requested
            Dispatcher.InvokeOnRender(() => ChatListBox?.ScrollToBottom());
        }



        protected override void OnClosed(EventArgs e)
        {
            // Stop generation if window is closing while generating
            if (ViewModel.IsGenerating)
            {
                ViewModel.StopGeneration("窗口已关闭，生成已停止");
            }

            // Unregister window attachment for this window
            if (_codeEditorHandle != IntPtr.Zero && _chatWindowHandle != IntPtr.Zero)
            {
                _windowAttachService.Unregister(_codeEditorHandle, _chatWindowHandle);
            }

            ViewModel.Dispose();
            base.OnClosed(e);
        }
    }
}

