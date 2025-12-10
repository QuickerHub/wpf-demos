using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace ActionPathConvert.Controls
{
    /// <summary>
    /// FileEditorControl.xaml 的交互逻辑
    /// </summary>
    public partial class FileEditorControl : UserControl
    {
        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(
                nameof(FilePath),
                typeof(string),
                typeof(FileEditorControl),
                new PropertyMetadata(string.Empty, OnFilePathChanged));

        public static readonly DependencyProperty AutoSaveProperty =
            DependencyProperty.Register(
                nameof(AutoSave),
                typeof(bool),
                typeof(FileEditorControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(
                nameof(IsModified),
                typeof(bool),
                typeof(FileEditorControl),
                new PropertyMetadata(false));

        private bool _isLoading = false;
        private string _lastSavedContent = string.Empty;

        public FileEditorControl()
        {
            InitializeComponent();
            
            // Handle text changed event
            TextEditor.TextChanged += TextEditor_TextChanged;
            
            // Handle Ctrl+S to save
            TextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
        }

        /// <summary>
        /// File path to edit
        /// </summary>
        public string FilePath
        {
            get => (string)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Auto save when text changes
        /// </summary>
        public bool AutoSave
        {
            get => (bool)GetValue(AutoSaveProperty);
            set => SetValue(AutoSaveProperty, value);
        }

        /// <summary>
        /// Whether the file has been modified
        /// </summary>
        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            private set => SetValue(IsModifiedProperty, value);
        }

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FileEditorControl control)
            {
                // Only reload if the path actually changed, or if it's being set to the same path (force reload)
                var newPath = e.NewValue as string;
                var oldPath = e.OldValue as string;
                
                // Always reload if path is different, or if explicitly set to same path (for refresh)
                if (newPath != oldPath || (newPath == oldPath && !string.IsNullOrEmpty(newPath)))
                {
                    control.LoadFile(newPath);
                }
            }
        }
        
        /// <summary>
        /// Force reload the current file
        /// </summary>
        public void ReloadFile()
        {
            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                LoadFile(FilePath);
            }
        }

        private void LoadFile(string? filePath)
        {
            _isLoading = true;
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    TextEditor.Text = string.Empty;
                    _lastSavedContent = string.Empty;
                    IsModified = false;
                    return;
                }

                var content = File.ReadAllText(filePath);
                TextEditor.Text = content;
                _lastSavedContent = content;
                IsModified = false;
            }
            catch (Exception ex)
            {
                TextEditor.Text = $"Error loading file: {ex.Message}";
                _lastSavedContent = string.Empty;
                IsModified = false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TextEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_isLoading)
                return;

            var currentContent = TextEditor.Text ?? string.Empty;
            IsModified = currentContent != _lastSavedContent;

            // Auto save if enabled
            if (AutoSave && IsModified && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                SaveFile();
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+S to save
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SaveFile();
            }
        }

        /// <summary>
        /// Save the file
        /// </summary>
        public void SaveFile()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;

            try
            {
                var content = TextEditor.Text ?? string.Empty;
                File.WriteAllText(FilePath, content, System.Text.Encoding.UTF8);
                _lastSavedContent = content;
                IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

