using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Media;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for chat messages in the Agent conversation
/// </summary>
public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private ChatMessageType _messageType;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private string _icon = "💬";

    [ObservableProperty]
    private Brush _backgroundBrush = Brushes.White;

    [ObservableProperty]
    private Brush _foregroundBrush = Brushes.Black;

    [ObservableProperty]
    private ExpressionItemViewModel? _expressionItem;

    [ObservableProperty]
    private ToolCallViewModel? _toolCallItem;

    /// <summary>
    /// Whether this message should be visible in the UI
    /// </summary>
    [ObservableProperty]
    private bool _isVisible = true;

    public ChatMessageViewModel(ChatMessageType messageType, string content)
    {
        MessageType = messageType;
        Content = content;
        InitializeStyles();
    }

    public ChatMessageViewModel(ChatMessageType messageType, ExpressionItemViewModel expressionItem)
    {
        MessageType = messageType;
        ExpressionItem = expressionItem;
        Content = "表达式测试结果";
        InitializeStyles();
    }

    public ChatMessageViewModel(ChatMessageType messageType, ToolCallViewModel toolCallItem)
    {
        MessageType = messageType;
        ToolCallItem = toolCallItem;
        Content = "工具调用";
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        // Set icon and colors based on message type
        switch (MessageType)
        {
            case ChatMessageType.User:
                Icon = "👤";
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // Light gray
                ForegroundBrush = Brushes.Black;
                break;
            case ChatMessageType.Assistant:
                Icon = "🤖";
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Light gray
                ForegroundBrush = Brushes.Black;
                break;
            default:
                Icon = "💬";
                BackgroundBrush = Brushes.White;
                ForegroundBrush = Brushes.Black;
                break;
        }
        
        // Initialize visibility based on content
        UpdateVisibility();
    }

    /// <summary>
    /// Automatically update visibility when content changes
    /// </summary>
    partial void OnContentChanged(string value)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Update visibility based on content and message type
    /// </summary>
    private void UpdateVisibility()
    {
        // For Assistant messages, hide if content is empty or whitespace
        if (MessageType == ChatMessageType.Assistant)
        {
            IsVisible = !string.IsNullOrWhiteSpace(Content);
        }
        else
        {
            // User and Tool messages are always visible
            IsVisible = true;
        }
    }
}

/// <summary>
/// Types of chat messages
/// </summary>
public enum ChatMessageType
{
    User,           // User input
    Assistant,      // Assistant response
    Tool            // Tool call
}

