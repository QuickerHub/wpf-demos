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
        // Don't close completion window when typing inside braces
        if (e.Text.Length > 0 && _completionWindow != null && CompletionService != null)
        {
            var doc = TextEditor.Document;
            var offset = TextEditor.TextArea.Caret.Offset;
            
            // Check if cursor is inside braces
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition >= 0)
            {
                // Inside braces, don't close completion window
                return;
            }
            
            // Outside braces, check if character should close completion
            var ch = e.Text[0];
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
        
        // Check if this is a completion trigger character, or if cursor is inside braces
        if (triggerChar == '{' || triggerChar == '.' || triggerChar == ':')
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
        
        // Use StartOffset provided by service (service decides based on completion type)
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
        
        // Update StartOffset if service changed it
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
        // Use unified CompletionData class for all completion types
        // All completion logic (replacement text, cursor position) is determined by the service
        return new CompletionData
        {
            Text = item.Text,
            Content = item.DisplayText,
            Description = item.Description,
            ReplacementText = item.ReplacementText ?? item.DisplayText ?? item.Text,
            CursorOffset = item.CursorOffset,
            ReplaceStartOffset = context.ReplaceStartOffset,
            ReplaceEndOffset = context.ReplaceEndOffset
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
