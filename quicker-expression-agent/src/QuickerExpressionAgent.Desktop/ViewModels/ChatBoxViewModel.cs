using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Agent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using static QuickerExpressionAgent.Server.Agent.ExpressionAgent;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// Base ViewModel for chat functionality shared between ExpressionGeneratorPageViewModel and ChatWindowViewModel
/// </summary>
public abstract partial class ChatBoxViewModel : ObservableObject
{
    protected readonly ExpressionAgentViewModel _agentViewModel;
    protected readonly ILogger _logger;
    
    // Chat history managed by caller
    protected readonly ChatHistory _chatHistory = new();
    
    // Cancellation token source for stopping generation
    protected CancellationTokenSource? _cancellationTokenSource;

    // Chat messages and input
    [ObservableProperty]
    public partial ObservableCollection<ChatMessageViewModel> ChatMessages { get; set; } = new();

    [ObservableProperty]
    public partial string ChatInputText { get; set; } = string.Empty;

    // Subscription for scroll-to-bottom signal using DynamicData
    protected IDisposable? _scrollThrottleSubscription;

    // Event to signal that chat should scroll to bottom
    public event EventHandler? ChatScrollToBottomRequested;

    [ObservableProperty]
    protected string _statusText = "就绪";

    [ObservableProperty]
    protected bool _isGenerating = false;

    /// <summary>
    /// Expose ExpressionAgentViewModel for binding
    /// </summary>
    public ExpressionAgentViewModel AgentViewModel => _agentViewModel;

    protected ChatBoxViewModel(
        ExpressionAgentViewModel agentViewModel,
        ILogger logger)
    {
        _agentViewModel = agentViewModel;
        _logger = logger;

        // Listen for agent recreation events
        _agentViewModel.AgentRecreated += OnAgentRecreated;

        // Update status text from agent view model
        StatusText = _agentViewModel.StatusText;

        // Initialize chat messages monitoring for auto-scroll
        // Use ObserveOnDispatcher to ensure UI updates happen on UI thread
        _scrollThrottleSubscription = ChatMessages
            .ToObservableChangeSet()
            .Where(changes => changes.Any(c => c.Reason is ListChangeReason.Add or
                                               ListChangeReason.Replace))
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOnDispatcher()
            .Subscribe(_ => ChatScrollToBottomRequested?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Add a chat message to the conversation
    /// </summary>
    public void AddChatMessage(ChatMessageType messageType, string content)
    {
        // Skip empty messages (streaming API may return empty messages)
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }
        ChatMessages.Add(new ChatMessageViewModel(messageType, content));
        UpdateTokenUsage();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    protected async Task GenerateAsync()
    {
        var text = ChatInputText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return; // Button: don't allow empty text
        }

        // If already generating, cancel the current operation (stop + send combination)
        if (IsGenerating)
        {
            _cancellationTokenSource?.Cancel();
            StatusText = "正在停止...";
            // Wait a bit for cancellation to complete
            await Task.Delay(100);
        }

        ChatInputText = "";
        await GenerateInternalAsync(text);
    }

    /// <summary>
    /// Internal method to generate expression from text input
    /// Override this method in derived classes to customize behavior before/after generation
    /// </summary>
    protected virtual async Task GenerateInternalAsync(string text)
    {
        // Check if agent is initialized
        var agent = _agentViewModel.Agent;
        if (agent == null)
        {
            AddChatMessage(ChatMessageType.Assistant, "✗ Agent 未初始化，请先配置 API Key");
            return;
        }

        // Allow derived classes to perform pre-generation checks
        if (!CanGenerate())
        {
            return;
        }

        // Create new cancellation token source
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

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
            await foreach (var item in agent.GenerateExpressionAsStreamAsync(text, _chatHistory, cancellationToken))
            {
                // Check if cancellation was requested
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
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
                                UpdateTokenUsage();
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
                                UpdateTokenUsage();
                                
                                // Subscribe to property changes for real-time updates
                                PropertyChangedEventHandler handler = (s, e) => FunctionCallItem_PropertyChanged(
                                    e, functionCallItem, toolCallViewModel);
                                functionCallItem.PropertyChanged += handler;
                                
                                functionCallMessages[functionCallItem.FunctionCallId] = (toolCallMessage, toolCallViewModel, functionCallItem);
                            }
                            break;

                        case TokenUsageStreamItem tokenUsageItem:
                            // Update token usage display with actual API usage information
                            UpdateTokenUsageFromStreamItem(tokenUsageItem);
                            break;
                    }
                });
            }

            RunOnUIThread(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    StatusText = "已停止生成";
                }
                else
                {
                    StatusText = "Agent 已完成";
                }
            });
        }
        catch (OperationCanceledException)
        {
            RunOnUIThread(() =>
            {
                StatusText = "已停止生成";
            });
        }
        catch (Exception ex)
        {
            RunOnUIThread(() =>
            {
                StatusText = "发生错误";
                _logger?.LogError(ex, "Error generating expression");
                var assistantMessage = new ChatMessageViewModel(ChatMessageType.Assistant, $"发生异常: {ex.Message}");
                ChatMessages.Add(assistantMessage);
                UpdateTokenUsage();
            });
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
    /// Override this method to perform pre-generation checks (e.g., verify connections)
    /// Return false to prevent generation
    /// </summary>
    protected virtual bool CanGenerate()
    {
        return true;
    }

    /// <summary>
    /// Handle ContentStreamItem property changes for real-time UI updates
    /// </summary>
    protected virtual void ContentItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
                    // Update token usage when content changes
                    UpdateTokenUsage();
                }
            });
        }
    }
    
    /// <summary>
    /// Handle FunctionCallStreamItem property changes for real-time UI updates
    /// </summary>
    protected virtual void FunctionCallItem_PropertyChanged(
        PropertyChangedEventArgs e,
        FunctionCallStreamItem functionCallItem,
        ToolCallViewModel toolCallViewModel)
    {
        RunOnUIThread(() =>
        {
            if (e.PropertyName == nameof(FunctionCallStreamItem.Arguments))
            {
                toolCallViewModel.Arguments = functionCallItem.Arguments;
                UpdateTokenUsage();
            }
            else if (e.PropertyName == nameof(FunctionCallStreamItem.Result))
            {
                toolCallViewModel.Result = functionCallItem.Result;
                // UpdateMarkdownContent will be called automatically by OnResultChanged
                UpdateTokenUsage();
            }
        });
    }

    /// <summary>
    /// Run action on UI thread
    /// </summary>
    protected void RunOnUIThread(Action action, DispatcherPriority priority = DispatcherPriority.Background)
    {
        Application.Current.Dispatcher.Invoke(action, priority);
    }

    /// <summary>
    /// Update token usage display based on current chat history
    /// Override in derived classes to implement token usage display
    /// </summary>
    protected virtual void UpdateTokenUsage()
    {
        // Default implementation does nothing
        // Derived classes can override to display token usage
    }

    /// <summary>
    /// Update token usage display from TokenUsageStreamItem (actual API usage)
    /// Override in derived classes to implement token usage display
    /// </summary>
    protected virtual void UpdateTokenUsageFromStreamItem(TokenUsageStreamItem tokenUsageItem)
    {
        // Default implementation does nothing
        // Derived classes can override to display token usage
    }

    /// <summary>
    /// Handle agent recreation events
    /// Override in derived classes to customize behavior
    /// </summary>
    protected virtual void OnAgentRecreated(object? sender, ExpressionAgent? agent)
    {
        // Update status text from agent view model
        StatusText = _agentViewModel.StatusText;
    }

    /// <summary>
    /// Cleanup resources
    /// </summary>
    public virtual void Dispose()
    {
        if (_agentViewModel != null)
        {
            _agentViewModel.AgentRecreated -= OnAgentRecreated;
        }
        _scrollThrottleSubscription?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}

