using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using static QuickerExpressionAgent.Server.Agent.ExpressionAgent;

namespace QuickerExpressionAgent.Desktop.ViewModels
{
    public partial class ChatWindowViewModel : ObservableObject
    {
        private readonly ExpressionAgent _agent;
        private readonly QuickerServerClientConnector _connector;
        private readonly ILogger<ChatWindowViewModel> _logger;
        private readonly ServerToolHandler _defaultToolHandler;
        private QuickerCodeEditorToolHandler? _quickerToolHandler;

        // Chat messages and input
        [ObservableProperty]
        public partial ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; } = new();

        [ObservableProperty]
        public partial string ChatInputText { get; set; } = string.Empty;

        // Subscription for scroll-to-bottom signal using DynamicData
        private IDisposable? _scrollThrottleSubscription;

        // Event to signal that chat should scroll to bottom
        public event EventHandler? ChatScrollToBottomRequested;

        [ObservableProperty]
        private string _statusText = "正在连接 Quicker 服务...";

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private bool _isConnected = false;

        public ChatWindowViewModel(
            IConfigurationService configurationService,
            ExpressionExecutor executor,
            QuickerServerClientConnector connector,
            ILogger<ChatWindowViewModel> logger)
        {
            _connector = connector;
            _logger = logger;
            
            // Create default tool handler using script engine (ServerToolHandler)
            // This is independent of Quicker connection and uses ExpressionExecutor
            _defaultToolHandler = new ServerToolHandler(executor);
            
            // Create ExpressionAgent with default tool handler (ensures IExpressionAgentToolHandler is not null)
            var kernel = QuickerExpressionAgent.Server.Services.KernelService.GetKernel(configurationService);
            _agent = new ExpressionAgent(kernel, executor, _defaultToolHandler);

            // Initialize chat messages monitoring for auto-scroll
            // Use ObserveOnDispatcher to ensure UI updates happen on UI thread
            _scrollThrottleSubscription = ChatMessages
                .ToObservableChangeSet()
                .Where(changes => changes.Any(c => c.Reason is ListChangeReason.Add or
                                                   ListChangeReason.Replace))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOnDispatcher()
                .Subscribe(_ => ChatScrollToBottomRequested?.Invoke(this, EventArgs.Empty));

            // Monitor connection status
            _connector.ConnectionStatusChanged += Connector_ConnectionStatusChanged;

            // Initialize connection
            InitializeConnectionAsync().ConfigureAwait(false);

            // Initialize chat with welcome message
            AddChatMessage(ChatMessageType.Assistant, "正在连接 Quicker 服务，请稍候...");
        }

        private void Connector_ConnectionStatusChanged(object? sender, bool isConnected)
        {
            // Ensure UI updates happen on UI thread (connector may call from background thread)
            RunOnUIThread(() =>
            {
                // Only update IsConnected flag, don't set it to true here
                // IsConnected will be set to true only after tool handler is created
                if (!isConnected)
                {
                    IsConnected = false;
                    // Switch back to default tool handler on disconnect
                    if (_quickerToolHandler != null)
                    {
                        _agent.ToolHandler = _defaultToolHandler;
                        _quickerToolHandler = null;
                    }
                    StatusText = "未连接到 Quicker 服务";
                    AddChatMessage(ChatMessageType.Assistant, "✗ 与 Quicker 服务断开连接");
                }
                // When connected, don't update IsConnected here - wait for tool handler creation
            });
        }

        private async Task InitializeConnectionAsync()
        {
            try
            {
                // Wait for connection (similar to .Server project)
                var connected = await _connector.WaitConnectAsync(TimeSpan.FromSeconds(10));
                if (!connected)
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法连接到 Quicker 服务";
                        AddChatMessage(ChatMessageType.Assistant, "✗ 无法连接到 Quicker 服务，请确保 Quicker 应用程序正在运行");
                    });
                    return;
                }

                // Use GetOrCreateCodeEditorAsync to get or create Code Editor (similar to .Server project)
                try
                {
                    RunOnUIThread(() =>
                    {
                        StatusText = "正在打开 Code Editor...";
                    });

                    var handlerId = await _connector.ServiceClient.GetOrCreateCodeEditorAsync();
                    
                    if (string.IsNullOrEmpty(handlerId) || handlerId == "standalone")
                    {
                        RunOnUIThread(() =>
                        {
                            StatusText = "无法创建 Code Editor";
                            AddChatMessage(ChatMessageType.Assistant, "✗ 无法创建 Code Editor 窗口");
                        });
                        return;
                    }

                    // Create handler using handlerId (similar to .Server project)
                    // Replace standalone tool handler with Quicker tool handler
                    _quickerToolHandler = new QuickerCodeEditorToolHandler(handlerId, _connector);
                    _agent.ToolHandler = _quickerToolHandler;

                    RunOnUIThread(() =>
                    {
                        IsConnected = true; // Update connection status
                        StatusText = "已就绪，可以开始生成表达式";
                        AddChatMessage(ChatMessageType.Assistant, $"✓ Code Editor 已打开 (Handler ID: {handlerId})，可以开始生成表达式");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting or creating Code Editor");
                    RunOnUIThread(() =>
                    {
                        StatusText = "无法创建 Code Editor";
                        AddChatMessage(ChatMessageType.Assistant, $"✗ 无法创建 Code Editor 窗口: {ex.Message}");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing connection");
                RunOnUIThread(() =>
                {
                    StatusText = "初始化失败";
                    AddChatMessage(ChatMessageType.Assistant, $"✗ 初始化失败: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Add a chat message to the conversation
        /// </summary>
        public void AddChatMessage(ChatMessageType messageType, string content)
        {
            ChatMessages.Add(new ChatMessageViewModel(messageType, content));
        }

        [RelayCommand]
        private async Task GenerateAsync()
        {
            var text = ChatInputText.Trim();
            ChatInputText = "";
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Check if Quicker tool handler is available (prefer Quicker over standalone)
            if (_quickerToolHandler == null)
            {
                AddChatMessage(ChatMessageType.Assistant, "✗ Code Editor 未就绪，无法生成表达式");
                return;
            }

            // Update UI state
            IsGenerating = true;
            StatusText = "正在生成表达式...";

            // Add user message
            AddChatMessage(ChatMessageType.User, text);

            // Track stream items for property change notifications
            ContentStreamItem? currentContentItem = null;
            Dictionary<string, (ChatMessageViewModel message, ToolCallViewModel viewModel, FunctionCallStreamItem item)> functionCallMessages = new();

            try
            {
                // Use GenerateExpressionAsStreamAsync to get stream items
                // Ensure UI updates happen on UI thread
                await foreach (var item in _agent.GenerateExpressionAsStreamAsync(text))
                {
                    // Update UI on UI thread
                    RunOnUIThread(() =>
                    {
                        switch (item)
                        {
                            case ContentStreamItem contentItem:
                                // Track content item and subscribe to property changes for real-time updates
                                if (currentContentItem != contentItem)
                                {
                                    // Unsubscribe from previous item if exists
                                    if (currentContentItem != null)
                                    {
                                        currentContentItem.PropertyChanged -= ContentItem_PropertyChanged;
                                    }
                                    
                                    // Subscribe to new item's property changes
                                    currentContentItem = contentItem;
                                    contentItem.PropertyChanged += ContentItem_PropertyChanged;

                                    // Set initial value
                                    var assistantMessage = new ChatMessageViewModel(ChatMessageType.Assistant, contentItem.Text);
                                    ChatMessages.Add(assistantMessage);
                                }
                                break;

                            case FunctionCallStreamItem functionCallItem:
                                // Track function call item and subscribe to property changes for real-time updates
                                if (!functionCallMessages.ContainsKey(functionCallItem.FunctionCallId))
                                {
                                    // New function call - create message and subscribe
                                    var toolCallViewModel = new ToolCallViewModel
                                    {
                                        FunctionName = functionCallItem.FunctionName,
                                        Arguments = functionCallItem.Arguments,
                                        FunctionCallId = functionCallItem.FunctionCallId,
                                        Result = functionCallItem.Result
                                    };
                                    var toolCallMessage = new ChatMessageViewModel(ChatMessageType.Tool, toolCallViewModel);
                                    ChatMessages.Add(toolCallMessage);
                                    
                                    // Subscribe to property changes for real-time updates
                                    PropertyChangedEventHandler handler = (s, e) => FunctionCallItem_PropertyChanged(
                                        e, functionCallItem, toolCallViewModel);
                                    functionCallItem.PropertyChanged += handler;
                                    
                                    functionCallMessages[functionCallItem.FunctionCallId] = (toolCallMessage, toolCallViewModel, functionCallItem);
                                }
                                break;
                        }
                    });
                }

                StatusText = "Agent 已完成";
            }
            catch (Exception ex)
            {
                StatusText = "发生错误";
                _logger?.LogError(ex, "Error generating expression");
                var assistantMessage = new ChatMessageViewModel(ChatMessageType.Assistant, $"发生异常: {ex.Message}");
                ChatMessages.Add(assistantMessage);
            }
            finally
            {
                // Cleanup: unsubscribe from property changes
                if (currentContentItem != null)
                {
                    currentContentItem.PropertyChanged -= ContentItem_PropertyChanged;
                }
                IsGenerating = false;
            }
        }
        
        /// <summary>
        /// Handle ContentStreamItem property changes for real-time UI updates
        /// </summary>
        private void ContentItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContentStreamItem.Text) && sender is ContentStreamItem item)
            {
                RunOnUIThread(() =>
                {
                    // Find the last assistant message (the one we're currently updating)
                    var assistantMessage = ChatMessages.LastOrDefault(m => m.MessageType == ChatMessageType.Assistant);
                    if (assistantMessage != null)
                    {
                        assistantMessage.Content = item.Text;
                    }
                });
            }
        }
        
        /// <summary>
        /// Handle FunctionCallStreamItem property changes for real-time UI updates
        /// </summary>
        private void FunctionCallItem_PropertyChanged(
            System.ComponentModel.PropertyChangedEventArgs e,
            FunctionCallStreamItem functionCallItem,
            ToolCallViewModel toolCallViewModel)
        {
            RunOnUIThread(() =>
            {
                if (e.PropertyName == nameof(FunctionCallStreamItem.Arguments))
                {
                    toolCallViewModel.Arguments = functionCallItem.Arguments;
                }
                else if (e.PropertyName == nameof(FunctionCallStreamItem.Result))
                {
                    toolCallViewModel.Result = functionCallItem.Result;
                    // UpdateMarkdownContent will be called automatically by OnResultChanged
                }
            });
        }

        /// <summary>
        /// Run action on UI thread
        /// </summary>
        private void RunOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.Background)
        {
            Application.Current.Dispatcher.Invoke(action, priority);
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _scrollThrottleSubscription?.Dispose();
            _connector.ConnectionStatusChanged -= Connector_ConnectionStatusChanged;
        }
    }
}

