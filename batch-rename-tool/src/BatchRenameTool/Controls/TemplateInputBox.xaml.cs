using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BatchRenameTool.MenuBuilders;
using DependencyPropertyGenerator;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace BatchRenameTool.Controls;

/// <summary>
/// Input control with variable menu and AvalonEdit text editor
/// </summary>
[DependencyProperty<string>("Text", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.TwoWay)]
[DependencyProperty<ICompletionService>("CompletionService")]
public partial class TemplateInputBox : UserControl
{
    private bool _isUpdatingText;
    private CompletionWindow? _completionWindow;
    private CompletionContext? _currentCompletionContext;

    public TemplateInputBox()
    {
        InitializeComponent();
        
        // Initialize document
        TextEditor.Document = new TextDocument();
        
        // Handle text changes from editor
        TextEditor.TextChanged += TextEditor_TextChanged;
        
        // Handle text input to show completion
        TextEditor.TextArea.TextEntering += TextArea_TextEntering;
        TextEditor.TextArea.TextEntered += TextArea_TextEntered;
        
        Loaded += TemplateInputBox_Loaded;
    }

    partial void OnCompletionServiceChanged(ICompletionService? oldValue, ICompletionService? newValue)
    {
        // Completion service changed, close any existing completion window
        CloseCompletionWindow();
    }

    private void TemplateInputBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Build variable menu
        BuildVariableMenu();
        
        // Sync TextEditor with Text property
        if (!string.IsNullOrEmpty(Text) && TextEditor.Document.Text != Text)
        {
            _isUpdatingText = true;
            try
            {
                TextEditor.Document.Text = Text;
            }
            finally
            {
                _isUpdatingText = false;
            }
        }
        
        // Handle Text property changes
        OnTextChanged(Text, Text);
    }

    partial void OnTextChanged(string oldValue, string newValue)
    {
        if (_isUpdatingText || TextEditor == null)
            return;

        if (TextEditor.Document.Text != newValue)
        {
            _isUpdatingText = true;
            try
            {
                var caretOffset = TextEditor.CaretOffset;
                TextEditor.Document.BeginUpdate();
                try
                {
                    TextEditor.Document.Text = newValue ?? string.Empty;
                }
                finally
                {
                    TextEditor.Document.EndUpdate();
                }
                // Restore cursor position if possible
                if (caretOffset <= TextEditor.Document.TextLength)
                {
                    TextEditor.CaretOffset = caretOffset;
                }
            }
            finally
            {
                _isUpdatingText = false;
            }
        }
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingText)
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

        _isUpdatingText = true;
        try
        {
            if (Text != TextEditor.Document.Text)
            {
                Text = TextEditor.Document.Text;
                
                // Explicitly update binding source
                var bindingExpression = GetBindingExpression(TextProperty);
                bindingExpression?.UpdateSource();
            }
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
            return;

        var doc = TextEditor.Document;
        var offset = TextEditor.TextArea.Caret.Offset;
        var ch = e.Text[0];
        
        // Handle closing brackets: if cursor is already at the closing bracket, skip input to avoid duplicate
        if (ch == '}' || ch == ')' || ch == ']')
        {
            if (offset < doc.TextLength && doc.GetCharAt(offset) == ch)
            {
                e.Handled = true;
                TextEditor.TextArea.Caret.Offset = offset + 1;
                return;
            }
        }
        
        // Don't close completion window when typing inside braces
        if (_completionWindow != null && CompletionService != null)
        {
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition >= 0)
            {
                return;
            }
            
            if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '{' && ch != '(' && ch != '[' && ch != '.' && ch != ':')
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
        
        // Handle opening brackets: automatically add closing brackets
        if (triggerChar == '{' || triggerChar == '(' || triggerChar == '[')
        {
            _isUpdatingText = true;
            try
            {
                if (offset <= doc.TextLength)
                {
                    // Insert corresponding closing bracket
                    string closingBracket = triggerChar switch
                    {
                        '{' => "}",
                        '(' => ")",
                        '[' => "]",
                        _ => ""
                    };
                    
                    if (!string.IsNullOrEmpty(closingBracket))
                    {
                        doc.Insert(offset, closingBracket);
                        TextEditor.TextArea.Caret.Offset = offset;
                        
                        // For '{', trigger completion
                        if (triggerChar == '{')
                        {
                            var updatedText = doc.Text;
                            var context = CompletionService.GetCompletionContext(updatedText, offset, triggerChar);
                            
                            if (context != null && context.Items.Count > 0)
                            {
                                ShowCompletion(context);
                            }
                        }
                    }
                }
            }
            finally
            {
                _isUpdatingText = false;
            }
            return;
        }
        
        // Check if this is a completion trigger character
        if (triggerChar == '.' || triggerChar == ':')
        {
            var context = CompletionService.GetCompletionContext(doc.Text, offset, triggerChar);
            
            if (context != null && context.Items.Count > 0)
            {
                ShowCompletion(context);
            }
        }
        else
        {
            // Check if cursor is inside braces
            var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
            if (bracePosition >= 0)
            {
                var context = CompletionService.GetCompletionContext(doc.Text, offset, triggerChar);
                
                if (context != null && context.Items.Count > 0)
                {
                    ShowCompletion(context);
                }
            }
        }
    }

    private void CheckAndCloseCompletionWindow()
    {
        if (_completionWindow == null || _currentCompletionContext == null || CompletionService == null)
            return;

        var doc = TextEditor.Document;
        var offset = TextEditor.TextArea.Caret.Offset;
        var bracePosition = CompletionService.IsInsideBraces(doc.Text, offset);
        
        if (bracePosition < 0)
        {
            CloseCompletionWindow();
        }
    }

    private void ShowCompletion(CompletionContext context)
    {
        if (CompletionService == null)
            return;

        CloseCompletionWindow();

        if (context.Items.Count == 0)
            return;

        _currentCompletionContext = context;

        _completionWindow = new CompletionWindow(TextEditor.TextArea);
        
        // Remove border from completion window
        _completionWindow.BorderThickness = new Thickness(0);
        _completionWindow.BorderBrush = System.Windows.Media.Brushes.Transparent;
        _completionWindow.WindowStyle = WindowStyle.None;
        _completionWindow.AllowsTransparency = true;
        
        // Set background and foreground for dark mode
        var regionBrush = Application.Current.TryFindResource("RegionBrush");
        if (regionBrush is System.Windows.Media.Brush regionBrushValue)
        {
            _completionWindow.Background = regionBrushValue;
        }
        
        // Remove border and set styles for completion list
        if (_completionWindow.CompletionList != null)
        {
            _completionWindow.CompletionList.BorderThickness = new Thickness(0);
            
            // Set background and foreground for dark mode
            if (regionBrush is System.Windows.Media.Brush)
            {
                _completionWindow.CompletionList.Background = regionBrush as System.Windows.Media.Brush;
            }
            
            var primaryTextBrush = Application.Current.TryFindResource("PrimaryTextBrush");
            if (primaryTextBrush is System.Windows.Media.Brush primaryTextBrushValue)
            {
                _completionWindow.CompletionList.Foreground = primaryTextBrushValue;
            }
        }
        
        _completionWindow.Closed += (s, e) =>
        {
            _completionWindow = null;
            _currentCompletionContext = null;
        };

        // AvalonEdit uses text between StartOffset and caret for filtering
        _completionWindow.StartOffset = context.FilterStartOffset;

        foreach (var item in context.Items)
        {
            var completionData = CreateCompletionData(context, item);
            if (completionData != null)
            {
                _completionWindow.CompletionList.CompletionData.Add(completionData);
            }
        }

        _completionWindow.Show();

        if (_completionWindow.CompletionList.CompletionData.Count > 0)
        {
            _completionWindow.CompletionList.SelectedItem = _completionWindow.CompletionList.CompletionData[0];
        }
    }

    private void UpdateCompletionWindow(CompletionContext context)
    {
        if (_completionWindow == null)
            return;

        // Keep StartOffset in sync for proper filtering
        if (_completionWindow.StartOffset != context.FilterStartOffset)
        {
            _completionWindow.StartOffset = context.FilterStartOffset;
        }

        var completionList = _completionWindow.CompletionList;
        completionList.CompletionData.Clear();

        foreach (var item in context.Items)
        {
            var completionData = CreateCompletionData(context, item);
            if (completionData != null)
            {
                completionList.CompletionData.Add(completionData);
            }
        }

        if (completionList.CompletionData.Count > 0)
        {
            completionList.SelectedItem = completionList.CompletionData[0];
        }
    }

    private ICompletionData? CreateCompletionData(CompletionContext context, CompletionItem item)
    {
        var replaceOffset = context.FilterStartOffset - context.ReplaceStartOffset;
        var actualText = item.ReplacementText ?? item.DisplayText ?? item.Text;

        return new CompletionData
        {
            Text = item.Text,
            Content = item.DisplayText,
            Description = item.Description,
            ActualText = actualText,
            ReplaceOffset = replaceOffset,
            CompleteOffset = item.CursorOffset,
            ReplaceEndOffset = context.ReplaceEndOffset,
            Metadata = item.Metadata
        };
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

    private void BuildVariableMenu()
    {
        if (VariableMenu == null)
            return;

        VariableMenu.Items.Clear();

        var menuItems = VariableMenuBuilder.BuildVariableMenuItems((variableName, formatText) =>
        {
            // Insert pattern at cursor position
            string patternText;
            if (formatText != null)
            {
                patternText = $"{{{variableName}:{formatText}}}";
            }
            else
            {
                patternText = $"{{{variableName}}}";
            }

            InsertTextAtCursor(patternText);
        });

        foreach (var menuItem in menuItems)
        {
            VariableMenu.Items.Add(menuItem);
        }
    }

    private void AddVariableButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    /// <summary>
    /// Insert text at current cursor position
    /// </summary>
    private void InsertTextAtCursor(string text)
    {
        _isUpdatingText = true;
        try
        {
            var caretOffset = TextEditor.CaretOffset;
            var doc = TextEditor.Document;
            
            // Insert text at cursor position
            doc.Insert(caretOffset, text);
            
            // Move cursor after inserted text
            TextEditor.CaretOffset = caretOffset + text.Length;
            
            // Update Text property to trigger binding
            if (Text != doc.Text)
            {
                Text = doc.Text;
                
                // Explicitly update binding source
                var bindingExpression = GetBindingExpression(TextProperty);
                bindingExpression?.UpdateSource();
            }
            
            // Focus the editor
            TextEditor.Focus();
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    /// <summary>
    /// Focus the text editor
    /// </summary>
    public void FocusEditor()
    {
        TextEditor.Focus();
    }

    /// <summary>
    /// Select all text in the editor
    /// </summary>
    public void SelectAll()
    {
        TextEditor.SelectAll();
    }

    /// <summary>
    /// Get the text editor instance
    /// </summary>
    public ICSharpCode.AvalonEdit.TextEditor GetTextEditor()
    {
        return TextEditor;
    }
}
