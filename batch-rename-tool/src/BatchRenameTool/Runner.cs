using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BatchRenameTool.Services;
using BatchRenameTool.ViewModels;

namespace BatchRenameTool
{
    /// <summary>
    /// Runner for Quicker interface - provides entry points for Quicker actions
    /// </summary>
    public static class Runner
    {
        private static MainWindow? _mainWindow;

        /// <summary>
        /// Show main window (UI thread only, singleton)
        /// </summary>
        /// <param name="files">Optional list of file paths to add to the window</param>
        public static void ShowMainWindow(List<string>? files = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // If window exists and is not closed, activate it
                if (_mainWindow != null)
                {
                    // Check if window is still valid (not closed)
                    if (_mainWindow.IsLoaded)
                    {
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.Show();
                        _mainWindow.Activate();
                        
                        // Add files if provided
                        if (files != null && files.Count > 0)
                        {
                            AddFilesToWindow(files);
                        }
                        return;
                    }
                    else
                    {
                        // Window was closed, clear reference
                        _mainWindow = null;
                    }
                }

                // Create new window instance (singleton)
                _mainWindow = new MainWindow();

                // Remove reference when window is closed
                _mainWindow.Closed += (s, e) =>
                {
                    _mainWindow = null;
                };

                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Show();
                _mainWindow.Activate();
                
                // Add files if provided
                if (files != null && files.Count > 0)
                {
                    AddFilesToWindow(files);
                }
            });
        }

        /// <summary>
        /// Add files to the main window
        /// </summary>
        private static void AddFilesToWindow(List<string> files)
        {
            if (_mainWindow?.ViewModel == null)
                return;

            // Use ViewModel's AddFiles method to ensure preview is automatically updated
            _mainWindow.ViewModel.AddFiles(files);
        }

        /// <summary>
        /// Rename files using a template pattern
        /// </summary>
        /// <param name="filePaths">List of file paths to rename</param>
        /// <param name="pattern">Template pattern (e.g., "{name.upper()}", "{i:000}")</param>
        /// <returns>Rename result</returns>
        public static BatchRenameExecutor.RenameResult RenameFiles(List<string> filePaths, string pattern)
        {
            var service = new BatchRenameService();
            return service.RenameFiles(filePaths, pattern);
        }

        /// <summary>
        /// Rename files using provided new names
        /// </summary>
        /// <param name="filePaths">List of original file paths</param>
        /// <param name="newNames">List of new file names (must match filePaths count)</param>
        /// <returns>Rename result</returns>
        public static BatchRenameExecutor.RenameResult RenameFiles(List<string> filePaths, List<string> newNames)
        {
            var service = new BatchRenameService();
            return service.RenameFiles(filePaths, newNames);
        }
    }
}
