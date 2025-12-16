using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Runner for Quicker interface - provides entry points for Quicker actions
    /// </summary>
    public static class Runner
    {
        private static MainWindow? _mainWindow;
        private static readonly Dictionary<string, DiffEditorWindow> _diffEditorWindows = new Dictionary<string, DiffEditorWindow>();
        private static readonly Dictionary<string, EditorWindow> _editorWindows = new Dictionary<string, EditorWindow>();

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        public static void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // If window exists and is not closed, activate it
                if (_mainWindow != null && _mainWindow.IsLoaded)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    return;
                }

                // Create new window instance
                _mainWindow = new MainWindow();

                // Remove reference when window is closed
                _mainWindow.Closed += (s, e) =>
                {
                    _mainWindow = null;
                };

                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
            });
        }

        /// <summary>
        /// Show DiffEditor window with content
        /// </summary>
        /// <param name="originalText">Original text to display</param>
        /// <param name="modifiedText">Modified text to display</param>
        /// <param name="language">Language for syntax highlighting (default: plaintext)</param>
        /// <param name="editorId">Editor ID to reuse existing window. If null or empty, creates a new window</param>
        public static void ShowDiffEditor(string originalText, string modifiedText, string language = "plaintext", string? editorId = null)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Generate editor ID if not provided
                    if (string.IsNullOrEmpty(editorId))
                    {
                        editorId = Guid.NewGuid().ToString();
                    }

                    // Ensure editorId is not null (should not happen after above check, but for compiler)
                    var safeEditorId = editorId ?? Guid.NewGuid().ToString();

                    // Check if window with this ID already exists
                    if (_diffEditorWindows.TryGetValue(safeEditorId, out var existingWindow))
                    {
                        // Window exists, update content and show it
                        if (existingWindow.IsLoaded)
                        {
                            await existingWindow.UpdateContentAsync(originalText, modifiedText, language);
                            existingWindow.WindowState = WindowState.Normal;
                            existingWindow.Show();
                            existingWindow.Activate();
                            return;
                        }
                        else
                        {
                            // Window was closed, remove from dictionary
                            _diffEditorWindows.Remove(safeEditorId);
                        }
                    }

                    // Create new window
                    var window = new DiffEditorWindow();
                    window.Title = "Diff Editor";

                    // Remove from dictionary when window is closed
                    window.Closed += (s, e) =>
                    {
                        _diffEditorWindows.Remove(safeEditorId);
                    };

                    // Show window first, then initialize
                    window.WindowState = WindowState.Normal;
                    window.Show();
                    window.Activate();

                    // Initialize WebView after window is shown
                    await window.InitializeAsync(originalText, modifiedText, language);
                    _diffEditorWindows[safeEditorId] = window;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建 DiffEditor 窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        /// <summary>
        /// Show Code Editor window with content
        /// </summary>
        /// <param name="text">Text to display in editor</param>
        /// <param name="language">Language for syntax highlighting (default: plaintext)</param>
        /// <param name="editorId">Editor ID to reuse existing window. If null or empty, creates a new window</param>
        public static void ShowEditor(string text, string language = "plaintext", string? editorId = null)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Generate editor ID if not provided
                    if (string.IsNullOrEmpty(editorId))
                    {
                        editorId = Guid.NewGuid().ToString();
                    }

                    // Ensure editorId is not null (should not happen after above check, but for compiler)
                    var safeEditorId = editorId ?? Guid.NewGuid().ToString();

                    // Check if window with this ID already exists
                    if (_editorWindows.TryGetValue(safeEditorId, out var existingWindow))
                    {
                        // Window exists, update content and show it
                        if (existingWindow.IsLoaded)
                        {
                            await existingWindow.UpdateContentAsync(text, language);
                            existingWindow.WindowState = WindowState.Normal;
                            existingWindow.Show();
                            existingWindow.Activate();
                            return;
                        }
                        else
                        {
                            // Window was closed, remove from dictionary
                            _editorWindows.Remove(safeEditorId);
                        }
                    }

                    // Create new window
                    var window = new EditorWindow();
                    window.Title = "Code Editor";

                    // Remove from dictionary when window is closed
                    window.Closed += (s, e) =>
                    {
                        _editorWindows.Remove(safeEditorId);
                    };

                    // Show window first, then initialize
                    window.WindowState = WindowState.Normal;
                    window.Show();
                    window.Activate();

                    // Initialize WebView after window is shown
                    await window.InitializeAsync(text, language);
                    _editorWindows[safeEditorId] = window;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建 Code Editor 窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }
    }
}

