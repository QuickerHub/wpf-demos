using System.Collections.Concurrent;
using System.Windows;
using QuickerCodeEditor.View;
using Quicker.Public.Interfaces;

namespace QuickerCodeEditor
{
    /// <summary>
    /// Code editor runner for showing code editor windows
    /// </summary>
    public static class Runner
    {
        private static readonly ConcurrentDictionary<string, CodeEditorWrapper> _editorInstances = new();

        /// <summary>
        /// Show code editor window (singleton per ActionId)
        /// </summary>
        public static void ShowCodeEditor(IActionContext context)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var actionId = context.ActionId;
                
                // Get or create editor instance
                var codeEditor = _editorInstances.GetOrAdd(actionId, key =>
                {
                    var editor = new CodeEditorWrapper(context);
                    // Remove from dictionary when window is closed
                    editor.TheWindow.Closed += (s, e) =>
                    {
                        _editorInstances.TryRemove(actionId, out _);
                    };
                    return editor;
                });

                // Show and activate the window
                Show(codeEditor);
            });
        }

        /// <summary>
        /// Show and activate the code editor window
        /// </summary>
        public static void Show(CodeEditorWrapper codeEditor)
        {
            if (codeEditor == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                codeEditor.TheWindow.WindowState = WindowState.Normal;
                codeEditor.TheWindow.Show();
                codeEditor.TheWindow.Activate();
            });
        }

        /// <summary>
        /// Show main window (UI thread only)
        /// </summary>
        public static void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = new MainWindow();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Show();
                mainWindow.Activate();
            });
        }
    }
}

