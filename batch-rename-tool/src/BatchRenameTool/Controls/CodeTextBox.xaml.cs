using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls;

/// <summary>
/// Custom TextBox control based on AvalonEdit with variable completion support
/// Shows completion window when typing '{' character
/// </summary>
[DependencyProperty<string>("CodeText", DefaultValue = "")]
[DependencyProperty<IEnumerable<string>>("CompletionItems")]
public partial class CodeTextBox : UserControl
{
    private CompletionWindow? _completionWindow;
    private bool _isUpdatingFromCodeText;
    private bool _isUpdatingFromEditor;

    public CodeTextBox()
    {
        InitializeComponent();
        
        // Initialize document
        TextEditor.Document = new TextDocument();
        
        // Handle text changes from editor
        TextEditor.TextChanged += TextEditor_TextChanged;
        
        // Handle text input to show completion
        TextEditor.TextArea.TextEntering += TextArea_TextEntering;
        TextEditor.TextArea.TextEntered += TextArea_TextEntered;
        
        // Handle loaded event to set initial text
        Loaded += CodeTextBox_Loaded;
    }

    partial void OnCodeTextChanged(string? oldValue, string newValue)
    {
        if (_isUpdatingFromEditor)
            return;

        _isUpdatingFromCodeText = true;
        try
        {
            if (TextEditor.Document.Text != newValue)
            {
                TextEditor.Document.BeginUpdate();
                try
                {
                    TextEditor.Document.Text = newValue ?? string.Empty;
                }
                finally
                {
                    TextEditor.Document.EndUpdate();
                }
            }
        }
        finally
        {
            _isUpdatingFromCodeText = false;
        }
    }

    partial void OnCompletionItemsChanged(IEnumerable<string>? oldValue, IEnumerable<string>? newValue)
    {
        // Completion items changed, close any existing completion window
        CloseCompletionWindow();
    }

    private void CodeTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Set initial text if CodeText is set before Loaded
        if (!string.IsNullOrEmpty(CodeText) && TextEditor.Document.Text != CodeText)
        {
            _isUpdatingFromCodeText = true;
            try
            {
                TextEditor.Document.Text = CodeText;
            }
            finally
            {
                _isUpdatingFromCodeText = false;
            }
        }
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingFromCodeText)
            return;

        _isUpdatingFromEditor = true;
        try
        {
            if (CodeText != TextEditor.Text)
            {
                CodeText = TextEditor.Text;
            }
        }
        finally
        {
            _isUpdatingFromEditor = false;
        }
    }

    private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
    {
        // Close completion window when user types a character that would close it
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
            {
                // If user types a non-letter/digit character (except _), close completion
                // But allow '{' to trigger completion
                if (e.Text[0] != '{')
                {
                    CloseCompletionWindow();
                }
            }
        }
    }

    private void TextArea_TextEntered(object? sender, TextCompositionEventArgs e)
    {
        // Show completion window when user types '{'
        if (e.Text == "{")
        {
            ShowCompletion();
        }
    }

    private void ShowCompletion()
    {
        // Close existing completion window if any
        CloseCompletionWindow();

        // Get completion items
        var items = CompletionItems?.ToList() ?? new List<string>();
        if (items.Count == 0)
        {
            return;
        }

        // Get current cursor position
        var textArea = TextEditor.TextArea;
        var caret = textArea.Caret;
        var offset = caret.Offset;
        
        // Check if there's a '{' character before cursor
        if (offset == 0 || TextEditor.Document.GetCharAt(offset - 1) != '{')
        {
            return;
        }

        // Create completion window
        _completionWindow = new CompletionWindow(textArea);
        
        // Set start offset to the position of '{'
        _completionWindow.StartOffset = offset - 1;
        
        // Add completion items
        var completionData = items.Select(item => new CompletionData
        {
            Text = item,
            Content = $"{{{item}}}",
            Description = GetVariableDescription(item),
            StartOffset = offset - 1 // Store the start offset for replacement
        }).ToList();

        // Add items one by one (IList doesn't have AddRange)
        foreach (var item in completionData)
        {
            _completionWindow.CompletionList.CompletionData.Add(item);
        }

        // Show completion window
        _completionWindow.Show();
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private void CloseCompletionWindow()
    {
        if (_completionWindow != null)
        {
            _completionWindow.Close();
            _completionWindow = null;
        }
    }

    private string GetVariableDescription(string variable)
    {
        return variable switch
        {
            "name" => "原文件名（不含扩展名）",
            "ext" => "文件扩展名（不含点号）",
            "fullname" => "完整文件名（包含扩展名）",
            _ => $"变量：{variable}"
        };
    }

    /// <summary>
    /// Gets the underlying TextEditor instance for advanced operations
    /// </summary>
    public TextEditor Editor => TextEditor;
}

/// <summary>
/// Completion data for variable completion
/// </summary>
internal class CompletionData : ICompletionData
{
    public string Text { get; set; } = string.Empty;
    public object Content { get; set; } = string.Empty;
    public object Description { get; set; } = string.Empty;
    public int StartOffset { get; set; }
    public double Priority => 0;
    public System.Windows.Media.ImageSource? Image => null;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        // Replace the '{' character and insert the completion text (with braces)
        // completionSegment should start at the '{' character position
        var replacement = $"{{{Text}}}";
        textArea.Document.Replace(completionSegment, replacement);
        
        // Move cursor after the inserted text
        textArea.Caret.Offset = completionSegment.Offset + replacement.Length;
    }
}
