using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatchRenameTool.Controls;
using BatchRenameTool.Models;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading;

namespace BatchRenameTool.ViewModels
{
    /// <summary>
    /// ViewModel for batch rename tool
    /// </summary>
    public partial class BatchRenameViewModel : ObservableObject
    {
        private readonly TemplateParser _parser;
        private readonly TemplateEvaluator _evaluator;
        private readonly ConfigService _configService;
        private readonly PatternHistoryConfig _patternHistoryConfig;

        [ObservableProperty]
        public partial ObservableCollection<FileRenameItem> Items { get; set; } = new();

        [ObservableProperty]
        public partial string RenamePattern { get; set; } = "";

        /// <summary>
        /// History of rename operations for undo functionality
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<RenameHistoryEntry> _renameHistory = new();

        /// <summary>
        /// Completion service for variable and method completion
        /// </summary>
        public ICompletionService CompletionService { get; } = new TemplateCompletionService();

        /// <summary>
        /// Status message for rename operations
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "共 0 个文件";

        /// <summary>
        /// Pattern history configuration
        /// </summary>
        public PatternHistoryConfig PatternHistoryConfig => _patternHistoryConfig;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BatchRenameViewModel(TemplateParser parser)
        {
            _parser = parser;
            _evaluator = new TemplateEvaluator();
            _configService = new ConfigService();
            _patternHistoryConfig = _configService.GetConfig<PatternHistoryConfig>();
            
            // Listen to history collection changes to update CanUndo
            RenameHistory.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanUndo));
                UndoRenameCommand.NotifyCanExecuteChanged();
            };
            
            // Listen to Items collection changes to update status
            Items.CollectionChanged += (s, e) =>
            {
                UpdateStatus();
                // Subscribe to NewName changes for new items
                if (e.NewItems != null)
                {
                    foreach (FileRenameItem item in e.NewItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                }
                // Unsubscribe from removed items
                if (e.OldItems != null)
                {
                    foreach (FileRenameItem item in e.OldItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }
            };
            
            // Subscribe to existing items
            SubscribeToItems();
        }
        
        /// <summary>
        /// Subscribe to NewName property changes for all items
        /// </summary>
        private void SubscribeToItems()
        {
            foreach (var item in Items)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        
        /// <summary>
        /// Handle property changes from FileRenameItem
        /// </summary>
        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileRenameItem.NewName))
            {
                UpdateStatus();
            }
        }
        
        /// <summary>
        /// Update status message based on current items
        /// </summary>
        private void UpdateStatus()
        {
            if (Items.Count == 0)
            {
                StatusMessage = "共 0 个文件";
                return;
            }
            
            var changedCount = Items.Count(item => item.IsNameChanged);
            var unchangedCount = Items.Count - changedCount;
            
            // Check for duplicate names (case-insensitive)
            var duplicateGroups = Items
                .Where(item => !string.IsNullOrWhiteSpace(item.NewName))
                .GroupBy(item => item.NewName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            
            var duplicateCount = duplicateGroups.Sum(g => g.Count());
            
            var statusParts = new List<string>();
            statusParts.Add($"共 {Items.Count} 个文件");
            
            if (changedCount > 0)
            {
                statusParts.Add($"{changedCount} 个将改变");
            }
            
            if (unchangedCount > 0)
            {
                statusParts.Add($"{unchangedCount} 个未改变");
            }
            
            if (duplicateCount > 0)
            {
                statusParts.Add($"⚠ {duplicateCount} 个重复名称");
            }
            
            StatusMessage = string.Join("，", statusParts);
        }

        /// <summary>
        /// Add files to rename list from folder path
        /// </summary>
        [RelayCommand]
        private void AddFiles(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath);
            AddFilesToList(files);
        }

        /// <summary>
        /// Public method to add files to the list (for external use, e.g., Runner)
        /// This method ensures preview is automatically updated
        /// </summary>
        /// <param name="filePaths">List of file paths to add</param>
        public void AddFiles(IEnumerable<string> filePaths)
        {
            if (filePaths == null)
                return;

            var files = filePaths.Where(File.Exists).ToArray();
            if (files.Length == 0)
                return;

            AddFilesToList(files);
        }

        /// <summary>
        /// Add files using file dialog (supports multiple selection)
        /// </summary>
        [RelayCommand]
        private void AddFilesFromDialog()
        {
            var fileNames = FileDialog.ShowOpenFileDialog(
                title: "选择文件",
                filter: "所有文件 (*.*)|*.*",
                filterIndex: 1);

            if (fileNames != null && fileNames.Length > 0)
            {
                AddFilesToList(fileNames);
            }
        }

        /// <summary>
        /// Paste files from clipboard (supports text and file drop list)
        /// </summary>
        [RelayCommand]
        private void PasteFiles()
        {
            try
            {
                var filesToAdd = new List<string>();
                
                // 1. Check for FileDrop format (Windows standard file drop list)
                if (System.Windows.Clipboard.ContainsData(System.Windows.DataFormats.FileDrop))
                {
                    var fileDropData = System.Windows.Clipboard.GetData(System.Windows.DataFormats.FileDrop);
                    if (fileDropData is string[] filePaths)
                    {
                        foreach (var filePath in filePaths)
                        {
                            if (File.Exists(filePath))
                            {
                                filesToAdd.Add(filePath);
                            }
                        }
                    }
                }
                
                // 2. Check for Quicker file drop list format
                var quickerFormats = new[] { "filedroplist", "FileDropList", "quicker-filedroplist" };
                foreach (var format in quickerFormats)
                {
                    if (System.Windows.Clipboard.ContainsData(format))
                    {
                        var data = System.Windows.Clipboard.GetData(format);
                        if (data is string[] paths)
                        {
                            foreach (var path in paths)
                            {
                                if (File.Exists(path))
                                {
                                    filesToAdd.Add(path);
                                }
                            }
                        }
                        else if (data is string text)
                        {
                            // Try to parse as newline-separated paths
                            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var trimmedLine = line.Trim().Trim('"', '\'');
                                if (File.Exists(trimmedLine))
                                {
                                    filesToAdd.Add(trimmedLine);
                                }
                            }
                        }
                        break; // Found and processed, no need to check other formats
                    }
                }
                
                // 3. Check for text content (fallback)
                if (filesToAdd.Count == 0 && System.Windows.Clipboard.ContainsText())
                {
                    var clipboardText = System.Windows.Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(clipboardText))
                    {
                        // Split by newlines and process each line
                        var lines = clipboardText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (string.IsNullOrEmpty(trimmedLine))
                                continue;

                            // Try to treat the line as a file path
                            // Remove quotes if present
                            trimmedLine = trimmedLine.Trim('"', '\'');

                            if (File.Exists(trimmedLine))
                            {
                                filesToAdd.Add(trimmedLine);
                            }
                        }
                    }
                }
                
                // Remove duplicates
                var uniqueFiles = filesToAdd.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                
                int addedCount = 0;
                int skippedCount = 0;
                
                // Check which files are already in the list
                var existingPaths = new HashSet<string>(Items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);
                
                foreach (var file in uniqueFiles)
                {
                    if (!existingPaths.Contains(file))
                    {
                        // Add file to list
                        var fileName = Path.GetFileName(file);

                        // Initially, new name is same as original (will be updated by UpdateRenamePreview)
                        Items.Add(new FileRenameItem
                        {
                            OriginalName = fileName,
                            NewName = fileName,
                            FullPath = file
                        });
                        addedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                
                if (addedCount > 0)
                {
                    var message = $"已从剪贴板添加 {addedCount} 个文件";
                    if (skippedCount > 0)
                    {
                        message += $"，跳过 {skippedCount} 个重复文件";
                    }
                    MessageHelper.ShowSuccess(message);
                    UpdateRenamePreview();
                }
                else if (filesToAdd.Count > 0)
                {
                    MessageHelper.ShowWarning($"剪贴板中有 {filesToAdd.Count} 个文件，但都是重复的或无效的");
                }
                else
                {
                    MessageHelper.ShowWarning("剪贴板中没有找到有效的文件路径");
                }
            }
            catch (Exception ex)
            {
                var message = $"从剪贴板粘贴文件失败: {ex.Message}";
                MessageHelper.ShowError(message);
            }
        }

        /// <summary>
        /// Helper method to add files to the list
        /// </summary>
        private void AddFilesToList(string[] files)
        {
            if (files == null || files.Length == 0)
                return;

            // Filter only existing files
            var existingFiles = files.Where(File.Exists).ToArray();
            if (existingFiles.Length == 0)
                return;

            // Sort files using natural sort order
            var comparer = new NaturalStringComparer();
            var sortedFiles = existingFiles.OrderBy(Path.GetFileName, comparer).ToArray();

            // Get existing paths to avoid duplicates
            var existingPaths = new HashSet<string>(Items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);

            int addedCount = 0;
            int skippedCount = 0;

            foreach (var file in sortedFiles)
            {
                // Skip if already in list
                if (existingPaths.Contains(file))
                {
                    skippedCount++;
                    continue;
                }

                var fileName = Path.GetFileName(file);

                // Initially, new name is same as original (will be updated by UpdateRenamePreview)
                Items.Add(new FileRenameItem
                {
                    OriginalName = fileName,
                    NewName = fileName,
                    FullPath = file
                });
                addedCount++;
            }

            if (addedCount > 0)
            {
                UpdateRenamePreview();
            }

            if (skippedCount > 0 && addedCount == 0)
            {
                MessageHelper.ShowInformation($"所有文件都已存在于列表中（{skippedCount} 个文件）");
            }
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
        /// Remove selected items from the list
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRemoveItems))]
        private void RemoveItems(object? selectedItems)
        {
            if (selectedItems is System.Collections.IList items)
            {
                var itemsToRemove = items.Cast<FileRenameItem>().ToList();
                foreach (var item in itemsToRemove)
                {
                    Items.Remove(item);
                }
                
                if (itemsToRemove.Count > 0)
                {
                    UpdateRenamePreview();
                }
            }
        }

        private bool CanRemoveItems(object? selectedItems)
        {
            return selectedItems is System.Collections.IList items && items.Count > 0;
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
        /// Uses asynchronous computation to avoid blocking UI when loading file properties
        /// </summary>
        private async void UpdateRenamePreview()
        {
            if (Items.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(RenamePattern))
            {
                // If pattern is empty, keep original names unchanged
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    item.NewName = item.OriginalName;
                }
                UpdateStatus();
                return;
            }

            try
            {
                // Parse template string into AST
                var templateNode = _parser.Parse(RenamePattern);

                // Check if template uses lazy-loaded properties (image, file, size)
                bool usesLazyProperties = RenamePattern.Contains("{image", StringComparison.OrdinalIgnoreCase) ||
                                         RenamePattern.Contains("{file", StringComparison.OrdinalIgnoreCase) ||
                                         RenamePattern.Contains("{size", StringComparison.OrdinalIgnoreCase);

                // Apply template to each file item
                int totalCount = Items.Count;
                
                if (usesLazyProperties)
                {
                    // For templates with lazy properties, compute asynchronously
                    await UpdateRenamePreviewAsync(templateNode, totalCount);
                }
                else
                {
                    // For simple templates, compute synchronously (faster)
                    UpdateRenamePreviewSync(templateNode, totalCount);
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
            
            // Update status after preview update
            UpdateStatus();
        }

        /// <summary>
        /// Synchronous preview update (for templates without lazy properties)
        /// </summary>
        private void UpdateRenamePreviewSync(TemplateNode templateNode, int totalCount)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var extension = Path.GetExtension(item.OriginalName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);

                // Create evaluation context for this file
                var context = new EvaluationContext(
                    name: nameWithoutExt,
                    ext: extension.TrimStart('.'), // Remove leading dot
                    fullName: item.OriginalName,
                    fullPath: item.FullPath,
                    index: i, // Index starts from 0
                    totalCount: totalCount, // Total count for reverse index calculation
                    fileRenameItem: item // Pass FileRenameItem to reuse ImageInfo
                );

                // Evaluate template with context
                var newName = _evaluator.Evaluate(templateNode, context);

                // Auto-add extension if template doesn't include it
                newName = AutoAddExtension(newName, extension);

                item.NewName = newName;
            }
        }

        /// <summary>
        /// Asynchronous preview update (for templates with lazy properties)
        /// </summary>
        private async Task UpdateRenamePreviewAsync(TemplateNode templateNode, int totalCount)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var extension = Path.GetExtension(item.OriginalName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);

                // Create evaluation context for this file
                var context = new EvaluationContext(
                    name: nameWithoutExt,
                    ext: extension.TrimStart('.'), // Remove leading dot
                    fullName: item.OriginalName,
                    fullPath: item.FullPath,
                    index: i, // Index starts from 0
                    totalCount: totalCount, // Total count for reverse index calculation
                    fileRenameItem: item // Pass FileRenameItem to reuse ImageInfo
                );
                
                // Evaluate template (lazy properties will be loaded on first access)
                var evaluateTask = Task.Run(() =>
                {
                    try
                    {
                        // Evaluate template with context
                        var newName = _evaluator.Evaluate(templateNode, context);

                        // Auto-add extension if template doesn't include it
                        newName = AutoAddExtension(newName, extension);

                        // Update on UI thread
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.NewName = newName;
                        });
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"执行错误: {ex.Message}";
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            item.NewName = $"[{errorMessage}]";
                        });
                    }
                });

                tasks.Add(evaluateTask);
            }

            // Wait for all evaluations to complete
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Auto-add extension if template doesn't include it
        /// </summary>
        private string AutoAddExtension(string newName, string extension)
        {
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
            return newName;
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
            
            // Create mapping between operations and items
            var operationItemMap = new Dictionary<BatchRenameExecutor.RenameOperation, FileRenameItem>();
            var operations = new List<BatchRenameExecutor.RenameOperation>();

            foreach (var item in Items)
            {
                var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
                var operation = new BatchRenameExecutor.RenameOperation
                {
                    OriginalPath = item.FullPath,
                    OriginalName = item.OriginalName,
                    NewName = item.NewName,
                    Directory = directory
                };
                operations.Add(operation);
                operationItemMap[operation] = item;
            }

            // Execute rename operations
            var result = executor.Execute(operations);

            // Build result message - focus on failures if any
            string message;
            string fullMessage;
            
            if (result.ErrorCount > 0 || result.ErrorDetails.Count > 0)
            {
                // If there are failures, focus on them
                var errorDetails = result.ErrorDetails;
                if (errorDetails.Count == 0 && result.Errors.Count > 0)
                {
                    // Fallback to old error format if ErrorDetails is empty
                    errorDetails = result.Errors.Select((error, index) => new BatchRenameExecutor.ErrorDetail
                    {
                        OriginalName = $"文件 {index + 1}",
                        NewName = "",
                        Reason = error
                    }).ToList();
                }

                var errorMessages = new List<string>();
                errorMessages.Add($"❌ 重命名失败: {result.ErrorCount} 个文件\n");
                
                foreach (var errorDetail in errorDetails)
                {
                    var fileName = Path.GetFileName(errorDetail.OriginalPath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = errorDetail.OriginalName;
                    }
                    errorMessages.Add($"  • {fileName}");
                    if (!string.IsNullOrEmpty(errorDetail.NewName) && errorDetail.NewName != errorDetail.OriginalName)
                    {
                        errorMessages.Add($"    目标名称: {errorDetail.NewName}");
                    }
                    errorMessages.Add($"    失败原因: {errorDetail.Reason}");
                    if (!string.IsNullOrEmpty(errorDetail.OriginalPath))
                    {
                        errorMessages.Add($"    文件路径: {errorDetail.OriginalPath}");
                    }
                    errorMessages.Add("");
                }

                // Add summary at the end
                var summaryParts = new List<string>();
                if (result.SuccessCount > 0)
                {
                    summaryParts.Add($"成功: {result.SuccessCount} 个");
                }
                if (result.SkippedCount > 0)
                {
                    summaryParts.Add($"跳过: {result.SkippedCount} 个");
                }
                if (summaryParts.Count > 0)
                {
                    errorMessages.Add($"\n总计: {string.Join("，", summaryParts)}");
                }

                fullMessage = string.Join("\n", errorMessages);
                MessageHelper.ShowError(fullMessage);
            }
            else
            {
                // No errors, show success summary
                var messageParts = new List<string>();
                if (result.SuccessCount > 0)
                {
                    messageParts.Add($"成功: {result.SuccessCount} 个文件");
                }
                if (result.SkippedCount > 0)
                {
                    messageParts.Add($"跳过: {result.SkippedCount} 个文件");
                }

                message = string.Join("，", messageParts);
                if (string.IsNullOrEmpty(message))
                {
                    message = "没有执行任何重命名操作";
                }

                fullMessage = message;
                if (result.SuccessCount > 0)
                {
                    MessageHelper.ShowSuccess(fullMessage);
                }
                else
                {
                    MessageHelper.ShowInformation(fullMessage);
                }
            }

            // Record successful renames for undo functionality
            var historyEntry = new RenameHistoryEntry
            {
                Timestamp = DateTime.Now,
                Operations = new List<SingleRenameOperation>()
            };

            // Update only successfully renamed items
            // Check each operation to see if it was successful
            foreach (var operation in operations)
            {
                if (!operationItemMap.TryGetValue(operation, out var item))
                    continue;

                var newFullPath = Path.Combine(operation.Directory, operation.NewName);
                var oldFullPath = operation.OriginalPath;

                // Check if rename was successful:
                // 1. New file exists
                // 2. Old file doesn't exist (or names are the same, meaning no rename was needed)
                bool wasRenamed = false;
                if (string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Same name, no rename needed
                    wasRenamed = true;
                }
                else if (File.Exists(newFullPath) && !File.Exists(oldFullPath))
                {
                    // Successfully renamed
                    wasRenamed = true;
                }

                if (wasRenamed)
                {
                    // Record for undo
                    historyEntry.Operations.Add(new SingleRenameOperation
                    {
                        Directory = operation.Directory,
                        OriginalName = operation.OriginalName,
                        NewName = operation.NewName,
                        OriginalFullPath = oldFullPath,
                        NewFullPath = newFullPath
                    });

                    // Update the item with new path and name
                    item.FullPath = newFullPath;
                    item.OriginalName = operation.NewName;
                }
            }

            // Add to history if there were any successful renames
            if (historyEntry.Operations.Count > 0)
            {
                RenameHistory.Add(historyEntry);
                OnPropertyChanged(nameof(CanUndo));
            }

            // Record pattern to history if rename was successful
            if (result.SuccessCount > 0 && !string.IsNullOrWhiteSpace(RenamePattern))
            {
                _patternHistoryConfig.AddPattern(RenamePattern);
            }

            // Refresh the preview for remaining items
            UpdateRenamePreview();
        }

        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo => RenameHistory.Count > 0;

        /// <summary>
        /// Undo the last rename operation
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void UndoRename()
        {
            if (RenameHistory.Count == 0)
                return;

            // Get the last history entry
            var lastEntry = RenameHistory[RenameHistory.Count - 1];

            // Create reverse operations (rename back to original names)
            var executor = new BatchRenameExecutor();
            var undoOperations = lastEntry.Operations.Select(op => new BatchRenameExecutor.RenameOperation
            {
                Directory = op.Directory,
                OriginalName = op.NewName,
                NewName = op.OriginalName,
                OriginalPath = op.NewFullPath
            }).ToList();

            // Execute undo
            var result = executor.Execute(undoOperations);

            // Update items list - match by the current file path (which is the new path after rename)
            foreach (var historyOp in lastEntry.Operations)
            {
                // Find the item that matches the new full path (the file we're reverting)
                var item = Items.FirstOrDefault(i => 
                    string.Equals(i.FullPath, historyOp.NewFullPath, StringComparison.OrdinalIgnoreCase));

                if (item != null)
                {
                    // After undo, the file should be at the original path
                    var originalFullPath = historyOp.OriginalFullPath;
                    if (File.Exists(originalFullPath))
                    {
                        item.FullPath = originalFullPath;
                        item.OriginalName = historyOp.OriginalName;
                    }
                }
            }

            // Remove from history
            RenameHistory.RemoveAt(RenameHistory.Count - 1);
            OnPropertyChanged(nameof(CanUndo));

            // Show result
            var message = result.SuccessCount > 0 
                ? $"已撤销 {result.SuccessCount} 个文件的重命名" 
                : "撤销操作未成功";
            
            var fullMessage = message + (result.Errors.Count > 0 ? "\n\n错误详情:\n" + string.Join("\n", result.Errors) : "");
            if (result.ErrorCount > 0)
            {
                MessageHelper.ShowWarning(fullMessage);
            }
            else if (result.SuccessCount > 0)
            {
                MessageHelper.ShowSuccess(fullMessage);
            }
            else
            {
                MessageHelper.ShowInformation(fullMessage);
            }

            // Refresh preview
            UpdateRenamePreview();
        }
    }

    /// <summary>
    /// Represents a file item for renaming
    /// </summary>
    public partial class FileRenameItem : ObservableObject
    {
        private Lazy<Template.Evaluator.ImageInfo>? _imageInfo;

        [ObservableProperty]
        public partial string OriginalName { get; set; } = "";

        [ObservableProperty]
        public partial string NewName { get; set; } = "";

        [ObservableProperty]
        public partial string FullPath { get; set; } = "";

        /// <summary>
        /// Image information (lazy loaded)
        /// </summary>
        public Template.Evaluator.IImageInfo Image
        {
            get
            {
                if (_imageInfo == null)
                {
                    _imageInfo = new Lazy<Template.Evaluator.ImageInfo>(
                        () => new Template.Evaluator.ImageInfo(FullPath),
                        System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
                }
                return _imageInfo.Value;
            }
        }

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

    /// <summary>
    /// Represents a single rename operation in history
    /// </summary>
    public class SingleRenameOperation
    {
        public string Directory { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string NewName { get; set; } = "";
        public string OriginalFullPath { get; set; } = "";
        public string NewFullPath { get; set; } = "";
    }

    /// <summary>
    /// Represents a history entry for a batch rename operation
    /// </summary>
    public class RenameHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public List<SingleRenameOperation> Operations { get; set; } = new();
    }
}
