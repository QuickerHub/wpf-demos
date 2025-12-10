using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BatchRenameTool.Controls;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BatchRenameTool.ViewModels
{
    /// <summary>
    /// ViewModel for batch rename tool
    /// </summary>
    public partial class BatchRenameViewModel : ObservableObject
    {
        private readonly TemplateParser _parser;
        private readonly TemplateEvaluator _evaluator;

        [ObservableProperty]
        public partial ObservableCollection<FileRenameItem> Items { get; set; } = new();

        [ObservableProperty]
        public partial string RenamePattern { get; set; } = "";

        /// <summary>
        /// Completion service for variable and method completion
        /// </summary>
        public ICompletionService CompletionService { get; } = new TemplateCompletionService();

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BatchRenameViewModel(TemplateParser parser)
        {
            _parser = parser;
            _evaluator = new TemplateEvaluator();
        }

        /// <summary>
        /// Add files to rename list
        /// </summary>
        [RelayCommand]
        private void AddFiles(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath);
            
            // Sort files using natural sort order
            var comparer = new NaturalStringComparer();
            var sortedFiles = files.OrderBy(Path.GetFileName, comparer).ToArray();

            foreach (var file in sortedFiles)
            {
                var fileName = Path.GetFileName(file);
                var fileExtension = Path.GetExtension(file);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                // Generate new name (for now, just add prefix as example)
                var newName = $"New_{fileNameWithoutExtension}{fileExtension}";

                Items.Add(new FileRenameItem
                {
                    OriginalName = fileName,
                    NewName = newName,
                    FullPath = file
                });
            }

            UpdateRenamePreview();
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        [RelayCommand]
        private void ClearItems()
        {
            Items.Clear();
        }

        /// <summary>
        /// Update rename preview based on pattern
        /// </summary>
        partial void OnRenamePatternChanged(string value)
        {
            UpdateRenamePreview();
        }

        /// <summary>
        /// Update preview of renamed files based on template pattern
        /// Supports advanced template features: variables, formatting, method calls, slicing
        /// </summary>
        private void UpdateRenamePreview()
        {
            if (Items.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(RenamePattern))
            {
                // Default: add prefix with extension preserved
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    var extension = Path.GetExtension(item.OriginalName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);
                    item.NewName = $"New_{nameWithoutExt}{extension}";
                }
                return;
            }

            try
            {
                // Parse template string into AST
                var templateNode = _parser.Parse(RenamePattern);

                // Apply template to each file item
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    var extension = Path.GetExtension(item.OriginalName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);

                    // Create evaluation context for this file
                    var context = new EvaluationContext
                    {
                        Name = nameWithoutExt,
                        Ext = extension.TrimStart('.'), // Remove leading dot
                        FullName = item.OriginalName,
                        Index = i // Index starts from 0
                    };

                    // Evaluate template with context
                    var newName = _evaluator.Evaluate(templateNode, context);

                    // Auto-add extension if template doesn't include it
                    // Check if the result contains a dot (likely extension) or if template explicitly includes {ext}
                    if (!string.IsNullOrEmpty(extension) && !newName.Contains("."))
                    {
                        // Check if template contains {ext} variable
                        bool templateHasExt = RenamePattern.Contains("{ext}", StringComparison.OrdinalIgnoreCase);
                        
                        // If template doesn't explicitly use {ext}, auto-add extension
                        if (!templateHasExt)
                        {
                            newName += extension;
                        }
                    }

                    item.NewName = newName;
                }
            }
            catch (ParseException ex)
            {
                // On parse error, show error message in preview for all items
                var errorMessage = $"解析错误: {ex.Message}";
                foreach (var item in Items)
                {
                    item.NewName = $"[{errorMessage}]";
                }
            }
            catch (Exception ex)
            {
                // On other errors (evaluation errors, etc.), show generic error
                var errorMessage = $"执行错误: {ex.Message}";
                foreach (var item in Items)
                {
                    item.NewName = $"[{errorMessage}]";
                }
            }
        }

        /// <summary>
        /// Execute rename operation using BatchRenameExecutor
        /// </summary>
        [RelayCommand]
        private void ExecuteRename()
        {
            if (Items.Count == 0)
                return;

            var executor = new BatchRenameExecutor();
            var operations = Items.Select(item =>
            {
                var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
                return new BatchRenameExecutor.RenameOperation
                {
                    OriginalPath = item.FullPath,
                    OriginalName = item.OriginalName,
                    NewName = item.NewName,
                    Directory = directory
                };
            }).ToList();

            var result = executor.Execute(operations);

            // Update items after rename
            foreach (var item in Items.ToList())
            {
                var directory = Path.GetDirectoryName(item.FullPath);
                if (directory == null)
                    continue;

                var newFullPath = Path.Combine(directory, item.NewName);
                if (File.Exists(newFullPath))
                {
                    item.FullPath = newFullPath;
                    item.OriginalName = item.NewName;
                }
            }

            // Refresh the list after rename
            UpdateRenamePreview();
        }
    }

    /// <summary>
    /// Represents a file item for renaming
    /// </summary>
    public partial class FileRenameItem : ObservableObject
    {
        [ObservableProperty]
        public partial string OriginalName { get; set; } = "";

        [ObservableProperty]
        public partial string NewName { get; set; } = "";

        [ObservableProperty]
        public partial string FullPath { get; set; } = "";

        /// <summary>
        /// Check if the name has been changed
        /// </summary>
        public bool IsNameChanged => OriginalName != NewName;

        partial void OnNewNameChanged(string value)
        {
            OnPropertyChanged(nameof(IsNameChanged));
        }

        partial void OnOriginalNameChanged(string value)
        {
            OnPropertyChanged(nameof(IsNameChanged));
        }
    }
}
