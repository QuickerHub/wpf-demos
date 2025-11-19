using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using DependencyPropertyGenerator;

namespace QuickerExpressionAgent.Desktop.Controls;

/// <summary>
/// Custom control wrapping AvalonEdit TextEditor with SuperText dependency property
/// Supports undoable editing
/// </summary>
[DependencyProperty<string>("SuperText", DefaultValue = "")]
public partial class SyntaxHighlightedTextBox : UserControl
{
    private bool _isUpdatingFromSuperText;
    private bool _isUpdatingFromEditor;

    public SyntaxHighlightedTextBox()
    {
        InitializeComponent();
        
        // Initialize document
        TextEditor.Document = new TextDocument();
        
        // Handle text changes from editor
        TextEditor.TextChanged += TextEditor_TextChanged;
        
        // Handle loaded event to set initial text
        Loaded += SyntaxHighlightedTextBox_Loaded;
    }

    partial void OnSuperTextChanged(string? oldValue, string newValue)
    {
        if (_isUpdatingFromEditor)
            return;

        _isUpdatingFromSuperText = true;
        try
        {
            // Use undoable way to set text
            if (TextEditor.Document.Text != newValue)
            {
                // Replace entire document content in an undoable way
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
            _isUpdatingFromSuperText = false;
        }
    }

    private void SyntaxHighlightedTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Set initial text if SuperText is set before Loaded
        if (!string.IsNullOrEmpty(SuperText) && TextEditor.Document.Text != SuperText)
        {
            _isUpdatingFromSuperText = true;
            try
            {
                TextEditor.Document.Text = SuperText;
            }
            finally
            {
                _isUpdatingFromSuperText = false;
            }
        }
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingFromSuperText)
            return;

        _isUpdatingFromEditor = true;
        try
        {
            if (SuperText != TextEditor.Text)
            {
                SuperText = TextEditor.Text;
            }
        }
        finally
        {
            _isUpdatingFromEditor = false;
        }
    }

    /// <summary>
    /// Gets or sets whether the text editor is read-only
    /// </summary>
    public bool IsReadOnly
    {
        get => TextEditor.IsReadOnly;
        set => TextEditor.IsReadOnly = value;
    }

    /// <summary>
    /// Gets or sets the syntax highlighting language (e.g., "C#", "XML", "JavaScript")
    /// </summary>
    public string? SyntaxHighlighting
    {
        get => TextEditor.SyntaxHighlighting?.Name;
        set => TextEditor.SyntaxHighlighting = value != null 
            ? ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition(value) 
            : null;
    }

    /// <summary>
    /// Gets or sets whether to show line numbers
    /// </summary>
    public bool ShowLineNumbers
    {
        get => TextEditor.ShowLineNumbers;
        set => TextEditor.ShowLineNumbers = value;
    }

    /// <summary>
    /// Gets the underlying TextEditor instance for advanced operations
    /// </summary>
    public TextEditor Editor => TextEditor;
}

