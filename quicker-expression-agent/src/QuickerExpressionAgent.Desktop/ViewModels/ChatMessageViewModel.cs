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
                BackgroundBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Blue
                ForegroundBrush = Brushes.White;
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

