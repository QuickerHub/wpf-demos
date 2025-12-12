using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls;

/// <summary>
/// Generic completion-enabled TextBox control based on AvalonEdit
/// All completion logic is handled by ICompletionService
/// </summary>
[DependencyProperty<string>("CodeText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.TwoWay)]
[DependencyProperty<ICompletionService>("CompletionService")]
public partial class CodeTextBox : UserControl
{
    private CompletionWindow? _completionWindow;
    private bool _isUpdatingFromCodeText;
    private bool _isUpdatingFromEditor;
    private CompletionContext? _currentCompletionContext;

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

    partial void OnCompletionServiceChanged(ICompletionService? oldValue, ICompletionService? newValue)
    {
        // Completion service changed, close any existing completion window
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

        // Update completion window if it's open
        if (_completionWindow != null && _currentCompletionContext != null && CompletionService != null)
        {
            var doc = TextEditor.Document;
            var offset = TextEditor.TextArea.Caret.Offset;
            
            // Ask service to update the completion context
            var updatedContext = CompletionService.UpdateCompletionContext(_currentCompletionContext, doc.Text, offset);
            
            if (updatedContext == null)
            {
                CloseCompletionWindow();
            }
            else
            {
                UpdateCompletionWindow(updatedContext);
                _currentCompletionContext = updatedContext;
            }
        }

        // Check if completion window should be closed
        CheckAndCloseCompletionWindow();

        _isUpdatingFromEditor = true;
        try
        {
            if (CodeText != TextEditor.Text)
            {
                CodeText = TextEditor.Text;
                
                // Explicitly update binding source to ensure ViewModel is notified
                var bindingExpression = GetBindingExpression(CodeTextProperty);
                bindingExpression?.UpdateSource();
            }
        }
        finally
        {
            _isUpdatingFromEditor = false;
        }
    }

    private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
            return;

        var doc = TextEditor.Document;
        var offset = TextEditor.TextArea.Caret.Offset;
        var ch = e.Text[0];
        
        // Handle '}' character: if cursor is already at '}', skip input to avoid duplicate
        if (ch == '}')
        {
            if (offset < doc.TextLength && doc.GetCharAt(offset) == '}')
            {
                // Cursor is already at '}', just move cursor forward
                e.Handled = true;
                TextEditor.TextArea.Caret.Offset = offset + 1;
                return;
            }
        }
        
        // Don't close completion window when typing inside braces
        if (_completionWindow != null && CompletionService != null)
        {
            // Check if cursor is inside braces
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition >= 0)
            {
                // Inside braces, don't close completion window
                return;
            }
            
            // Outside braces, check if character should close completion
            // Allow letters, digits, underscore, and trigger characters
            if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '{' && ch != '.' && ch != ':')
            {
                CloseCompletionWindow();
            }
        }
    }

    private void TextArea_TextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (CompletionService == null || e.Text.Length == 0)
            return;

        var doc = TextEditor.Document;
        var offset = TextEditor.TextArea.Caret.Offset;
        var triggerChar = e.Text[0];
        
        // Handle '{' character: automatically add closing '}'
        if (triggerChar == '{')
        {
            // Insert '}' after '{' to create {|}
            // The '{' has already been inserted by AvalonEdit, so offset is after '{'
            _isUpdatingFromEditor = true;
            try
            {
                if (offset <= doc.TextLength)
                {
                    // Insert '}' at current cursor position (after '{')
                    doc.Insert(offset, "}");
                    // After insertion, cursor automatically moves to after '}'
                    // We need to move it back to between { and } (at offset)
                    TextEditor.TextArea.Caret.Offset = offset;
                    
                    // Trigger completion for variable names
                    // Use the updated document text and current cursor position (between braces)
                    var updatedText = doc.Text;
                    var context = CompletionService.GetCompletionContext(updatedText, offset, triggerChar);
                    
                    if (context != null && context.Items.Count > 0)
                    {
                        ShowCompletion(context);
                    }
                }
            }
            finally
            {
                _isUpdatingFromEditor = false;
            }
            return;
        }
        
        // Check if this is a completion trigger character, or if cursor is inside braces
        if (triggerChar == '.' || triggerChar == ':')
        {
            // Ask service for completion context - service decides what type of completion to show
            var context = CompletionService.GetCompletionContext(doc.Text, offset, triggerChar);
            
            if (context != null && context.Items.Count > 0)
            {
                ShowCompletion(context);
            }
        }
        else
        {
            // Check if cursor is inside braces - if so, trigger variable completion
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition >= 0)
            {
                // Inside braces, trigger variable completion
                var context = CompletionService.GetCompletionContext(doc.Text, offset, triggerChar);
                
                if (context != null && context.Items.Count > 0)
                {
                    ShowCompletion(context);
                }
            }
        }
    }

    private void ShowCompletion(CompletionContext context)
    {
        CloseCompletionWindow();

        if (context.Items.Count == 0)
            return;

        var textArea = TextEditor.TextArea;
        _currentCompletionContext = context;

        // Create completion window
        _completionWindow = new CompletionWindow(textArea);
        
        // Remove border from completion window
        _completionWindow.BorderThickness = new Thickness(0);
        
        // Remove border from completion list
        if (_completionWindow.CompletionList != null)
        {
            _completionWindow.CompletionList.BorderThickness = new Thickness(0);
        }
        
        // Use FilterStartOffset for StartOffset to enable proper filtering
        // AvalonEdit uses text between StartOffset and caret position for filtering
        // FilterStartOffset is after '.' (for method completion) or after '{' (for variable completion)
        // We'll use ReplaceOffset in CompletionData.Complete to adjust the actual replacement start
        _completionWindow.StartOffset = context.FilterStartOffset;

        // Create completion data items based on type provided by service
        foreach (var item in context.Items)
        {
            var completionData = CreateCompletionData(context, item);
            if (completionData != null)
            {
                _completionWindow.CompletionList.CompletionData.Add(completionData);
            }
        }

        // Show completion window
        _completionWindow.Show();

        // Select first item automatically
        if (_completionWindow.CompletionList.CompletionData.Count > 0)
        {
            _completionWindow.CompletionList.SelectedItem = _completionWindow.CompletionList.CompletionData[0];
        }

        _completionWindow.Closed += (s, e) =>
        {
            _completionWindow = null;
            _currentCompletionContext = null;
        };
    }

    private void UpdateCompletionWindow(CompletionContext context)
    {
        if (_completionWindow == null)
            return;

        var completionList = _completionWindow.CompletionList;        
        
        // Use FilterStartOffset for StartOffset to enable proper filtering
        // AvalonEdit uses text between StartOffset and caret position for filtering
        // FilterStartOffset is after '.' (for method completion) or after '{' (for variable completion)
        // We'll use ReplaceOffset in CompletionData.Complete to adjust the actual replacement start
        if (_completionWindow.StartOffset != context.FilterStartOffset)
        {
            _completionWindow.StartOffset = context.FilterStartOffset;
        }

        // Clear and rebuild completion list based on context provided by service
        completionList.CompletionData.Clear();
        
        foreach (var item in context.Items)
        {
            var completionData = CreateCompletionData(context, item);
            if (completionData != null)
            {
                completionList.CompletionData.Add(completionData);
            }
        }

        // Select first item if available
        if (completionList.CompletionData.Count > 0)
        {
            completionList.SelectedItem = completionList.CompletionData[0];
        }
    }

    private ICompletionData? CreateCompletionData(CompletionContext context, CompletionItem item)
    {
        // Calculate replaceOffset: difference between FilterStartOffset and ReplaceStartOffset
        // Since StartOffset is set to ReplaceStartOffset, completionSegment.Offset will be ReplaceStartOffset
        // So ReplaceOffset should be 0, but we keep it for backward compatibility
        // Actually, if StartOffset is ReplaceStartOffset, we don't need ReplaceOffset
        // But to be safe, we calculate it as the difference
        var replaceOffset = context.FilterStartOffset - context.ReplaceStartOffset;

        // Actual text to insert
        var actualText = item.ReplacementText ?? item.DisplayText ?? item.Text;

        return new CompletionData
        {
            Text = item.Text,
            Content = item.DisplayText,
            Description = item.Description,
            ActualText = actualText,
            ReplaceOffset = replaceOffset,
            CompleteOffset = item.CursorOffset,
            ReplaceEndOffset = context.ReplaceEndOffset, // Limit replacement range
            Metadata = item.Metadata // Pass metadata for method completion
        };
    }

    private void CheckAndCloseCompletionWindow()
    {
        if (_completionWindow == null || _currentCompletionContext == null || CompletionService == null)
            return;

        try
        {
            var doc = TextEditor.Document;
            var offset = TextEditor.TextArea.Caret.Offset;

            // Check if cursor is still inside braces (service decides)
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition < 0)
            {
                // Cursor moved outside braces, close completion window
                CloseCompletionWindow();
                return;
            }

            // Cursor is still inside braces, keep completion window open
            // UpdateCompletionContext will handle filtering
        }
        catch
        {
            CloseCompletionWindow();
        }
    }

    private void CloseCompletionWindow()
    {
        if (_completionWindow != null)
        {
            _completionWindow.Close();
            _completionWindow = null;
            _currentCompletionContext = null;
        }
    }

    /// <summary>
    /// Gets the underlying TextEditor instance for advanced operations
    /// </summary>
    public TextEditor Editor => TextEditor;
}

/// <summary>
/// Extension methods for CodeTextBox operations
/// </summary>
internal static class CodeTextBoxExtensions
{
    /// <summary>
    /// Insert parentheses at the specified offset and move cursor inside
    /// </summary>
    public static void InsertParenthesesAndMoveCursor(TextArea textArea, int offset)
    {
        if (textArea?.Document == null)
            return;

        var doc = textArea.Document;
        
        // Ensure offset is valid
        if (offset < 0 || offset > doc.TextLength)
            return;

        // Insert "()" at the offset
        if (!doc.SafeInsert(offset, "()"))
            return;

        // Move cursor inside parentheses (after '(')
        var cursorOffset = offset + 1;
        textArea.SafeSetCaretOffset(cursorOffset);
    }
}
