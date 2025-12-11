using System;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;

namespace ActionPathConvert.Controls
{
    /// <summary>
    /// CodeTextEditor.xaml 的交互逻辑
    /// </summary>
    public partial class CodeTextEditor : UserControl
    {
        public static readonly DependencyProperty CodeTextProperty =
            DependencyProperty.Register(
                nameof(CodeText),
                typeof(string),
                typeof(CodeTextEditor),
                new PropertyMetadata(string.Empty, OnCodeTextChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(CodeTextEditor),
                new PropertyMetadata(false));

        private bool _isUpdatingFromTextEditor = false;

        public CodeTextEditor()
        {
            InitializeComponent();
            
            // Handle text changed from editor (for two-way binding if needed)
            TextEditor.TextChanged += TextEditor_TextChanged;
        }

        /// <summary>
        /// Text content of the editor
        /// </summary>
        public string CodeText
        {
            get => (string)GetValue(CodeTextProperty);
            set => SetValue(CodeTextProperty, value);
        }

        /// <summary>
        /// Whether the editor is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private static void OnCodeTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeTextEditor control && !control._isUpdatingFromTextEditor)
            {
                var newText = e.NewValue as string ?? string.Empty;
                if (control.TextEditor.Text != newText)
                {
                    control.TextEditor.Text = newText;
                }
            }
        }

        private void TextEditor_TextChanged(object? sender, EventArgs e)
        {
            if (!IsReadOnly)
            {
                _isUpdatingFromTextEditor = true;
                try
                {
                    CodeText = TextEditor.Text;
                }
                finally
                {
                    _isUpdatingFromTextEditor = false;
                }
            }
        }
    }
}

