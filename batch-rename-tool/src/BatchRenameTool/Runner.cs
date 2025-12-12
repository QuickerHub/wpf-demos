using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
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
            if (filePaths == null || filePaths.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                // Empty pattern means no rename
                return new BatchRenameExecutor.RenameResult
                {
                    SkippedCount = filePaths.Count
                };
            }

            // Filter valid files
            var validFiles = filePaths.Where(File.Exists).ToList();
            if (validFiles.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            // Create parser and evaluator
            var parser = new TemplateParser(new List<Type>());
            var evaluator = new TemplateEvaluator();
            var executor = new BatchRenameExecutor();

            // Parse template
            var templateNode = parser.Parse(pattern);

            // Generate rename operations
            var operations = new List<BatchRenameExecutor.RenameOperation>();
            int totalCount = validFiles.Count;

            for (int i = 0; i < validFiles.Count; i++)
            {
                var filePath = validFiles[i];
                var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                var originalName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(originalName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(originalName);

                // Create evaluation context
                var context = new EvaluationContext(
                    name: nameWithoutExt,
                    ext: extension.TrimStart('.'),
                    fullName: originalName,
                    fullPath: filePath,
                    index: i,
                    totalCount: totalCount);

                // Evaluate template
                var newName = evaluator.Evaluate(templateNode, context);

                // Auto-add extension if template doesn't include it
                if (!string.IsNullOrEmpty(extension) && !newName.Contains("."))
                {
                    bool templateHasExt = pattern.Contains("{ext}", StringComparison.OrdinalIgnoreCase);
                    if (!templateHasExt)
                    {
                        newName += extension;
                    }
                }

                operations.Add(new BatchRenameExecutor.RenameOperation
                {
                    OriginalPath = filePath,
                    OriginalName = originalName,
                    NewName = newName,
                    Directory = directory
                });
            }

            // Execute rename
            return executor.Execute(operations);
        }

        /// <summary>
        /// Rename files using provided new names
        /// </summary>
        /// <param name="filePaths">List of original file paths</param>
        /// <param name="newNames">List of new file names (must match filePaths count)</param>
        /// <returns>Rename result</returns>
        public static BatchRenameExecutor.RenameResult RenameFiles(List<string> filePaths, List<string> newNames)
        {
            if (filePaths == null || newNames == null)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            if (filePaths.Count != newNames.Count)
            {
                throw new ArgumentException($"File paths count ({filePaths.Count}) does not match new names count ({newNames.Count})");
            }

            // Filter valid files
            var validPairs = new List<(string filePath, string newName)>();
            for (int i = 0; i < filePaths.Count; i++)
            {
                if (File.Exists(filePaths[i]))
                {
                    validPairs.Add((filePaths[i], newNames[i]));
                }
            }

            if (validPairs.Count == 0)
            {
                return new BatchRenameExecutor.RenameResult();
            }

            // Create executor
            var executor = new BatchRenameExecutor();

            // Generate rename operations
            var operations = validPairs.Select(pair =>
            {
                var directory = Path.GetDirectoryName(pair.filePath) ?? string.Empty;
                var originalName = Path.GetFileName(pair.filePath);
                return new BatchRenameExecutor.RenameOperation
                {
                    OriginalPath = pair.filePath,
                    OriginalName = originalName,
                    NewName = pair.newName,
                    Directory = directory
                };
            }).ToList();

            // Execute rename
            return executor.Execute(operations);
        }
    }
}
