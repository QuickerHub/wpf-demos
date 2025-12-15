using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BatchRenameTool.Controls;
using BatchRenameTool.Models;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Ast;
using BatchRenameTool.Template.Compiler;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using System.Reactive.Linq;
using System.Windows.Threading;

namespace BatchRenameTool.ViewModels
{
    /// <summary>
    /// ViewModel for batch rename tool
    /// </summary>
    public partial class BatchRenameViewModel : ObservableObject
    {
        private readonly TemplateParser _parser;
        private readonly TemplateEvaluator _evaluator;
        private readonly TemplateCompiler _compiler;
        private readonly ConfigService _configService;
        private readonly PatternHistoryConfig _patternHistoryConfig;
        // Template compilation cache - cache compiled function instead of AST node
        private Func<IEvaluationContext, string>? _cachedCompiledFunction;

        // DynamicData SourceCache for efficient handling of large collections
        // Use FullPath as the stable key (FullPath 不可变)
        private readonly SourceCache<FileRenameItem, string> _itemsCache = new(item => item.FullPath);
        
        // ReadOnlyObservableCollection for binding to ListView
        private readonly ReadOnlyObservableCollection<FileRenameItem> _items;
        
        /// <summary>
        /// Observable collection of file items for binding to UI
        /// </summary>
        public ReadOnlyObservableCollection<FileRenameItem> Items => _items;

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
        /// Whether rename operation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isRenaming = false;

        /// <summary>
        /// Progress value (0-100)
        /// </summary>
        [ObservableProperty]
        private double _progressValue = 0;

        /// <summary>
        /// Progress text (e.g., "正在重命名: 5/100")
        /// </summary>
        [ObservableProperty]
        private string _progressText = "";

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
            _compiler = new TemplateCompiler();
            _configService = new ConfigService();
            _patternHistoryConfig = _configService.GetConfig<PatternHistoryConfig>();
            
            // Connect SourceCache to ReadOnlyObservableCollection for UI binding with sorting
            // Sort by OriginalName using natural sort order
            var naturalComparer = new NaturalStringComparer();
            var itemComparer = Comparer<FileRenameItem>.Create((x, y) => naturalComparer.Compare(x.OriginalName, y.OriginalName));
            
            _itemsCache.Connect()
                .SortAndBind(out _items, itemComparer)
                .Subscribe();
            
            // Listen to history collection changes to update CanUndo
            RenameHistory.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanUndo));
                UndoRenameCommand.NotifyCanExecuteChanged();
            };
            
            // Listen to Items cache changes to update status (with throttle)
            _itemsCache.Connect()
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(System.Windows.Application.Current.Dispatcher)
                .Subscribe(_ =>
                {
                    UpdateStatus();
                });
            
            // Subscribe to item property changes
            _itemsCache.Connect()
                .WhenPropertyChanged(item => item.NewName)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(System.Windows.Application.Current.Dispatcher)
                .Subscribe(_ =>
                {
                    UpdateStatus();
                });
            
            // Throttle rename pattern changes to reduce preview churn
            this.WhenValueChanged(vm => vm.RenamePattern)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(System.Windows.Application.Current.Dispatcher)
                .Subscribe(_ => UpdateRenamePreview());

            // DynamicData handles property changes automatically through WhenPropertyChanged
        }
        
        /// <summary>
        /// Update status message based on current items
        /// Optimized for large collections (10000+ items) using DynamicData
        /// </summary>
        private void UpdateStatus()
        {
            var count = _itemsCache.Count;
            if (count == 0)
            {
                StatusMessage = "共 0 个文件";
                return;
            }
            
            var statusParts = new List<string>();
            statusParts.Add($"共 {count:N0} 个文件");
            
            // Count files that will be renamed (name changed)
            int changedCount = 0;
            int errorCount = 0;
            int duplicateCount = 0;
            
            var items = _itemsCache.Items;
            
            // Count changed and error files
            foreach (var item in items)
            {
                // Check for errors
                if (string.IsNullOrEmpty(item.NewName) || item.NewName.StartsWith("["))
                {
                    errorCount++;
                }
                // Check if name changed (case-insensitive comparison)
                else if (!string.Equals(item.OriginalName, item.NewName, StringComparison.OrdinalIgnoreCase))
                {
                    changedCount++;
                }
            }
            
            // Check for duplicate new names (case-insensitive)
            var duplicateGroups = items
                .Where(item => !string.IsNullOrEmpty(item.NewName) && !item.NewName.StartsWith("["))
                .GroupBy(item => item.NewName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            
            duplicateCount = duplicateGroups.Sum(g => g.Count());
            
            // Add status information
            if (changedCount > 0)
            {
                statusParts.Add($"将重命名 {changedCount:N0} 个");
            }
            
            if (errorCount > 0)
            {
                statusParts.Add($"❌ {errorCount:N0} 个错误");
            }
            
            if (duplicateCount > 0)
            {
                statusParts.Add($"⚠ {duplicateCount:N0} 个重复名称");
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
        /// Reset files from folder (clear existing and add files from folder)
        /// </summary>
        [RelayCommand]
        private void ResetFilesFromFolder(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            // Clear existing items
            _itemsCache.Clear();

            // Add files from folder
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
                var existingPaths = new HashSet<string>(_itemsCache.Items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);
                
                foreach (var file in uniqueFiles)
                {
                    if (!existingPaths.Contains(file))
                    {
                        // Add file to list
                        var item = CreateFileRenameItem(file);
                        _itemsCache.AddOrUpdate(item);
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

        private static FileRenameItem CreateFileRenameItem(string fullPath)
        {
            var item = new FileRenameItem(fullPath);
            item.MarkForRecalculation();
            return item;
        }

        /// <summary>
        /// Helper method to add files to the list
        /// Optimized for large file lists (10000+ files)
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
            var existingPaths = new HashSet<string>(_itemsCache.Items.Select(i => i.FullPath), StringComparer.OrdinalIgnoreCase);

            // Prepare items to add in batch
            var itemsToAdd = new List<FileRenameItem>();
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

                // Create item but don't add yet (to avoid triggering CollectionChanged for each item)
                itemsToAdd.Add(CreateFileRenameItem(file));
            }

            // Batch add items to minimize CollectionChanged events
            if (itemsToAdd.Count > 0)
            {
                // Use DynamicData's batch operations for efficient adding
                // DynamicData handles large collections efficiently
                foreach (var item in itemsToAdd)
                {
                    item.MarkForRecalculation();
                }
                
                // Batch add all items at once (DynamicData is optimized for this)
                _itemsCache.AddOrUpdate(itemsToAdd);
                
                // Update preview only once after all items are added
                UpdateRenamePreview();
            }

            if (skippedCount > 0 && itemsToAdd.Count == 0)
            {
                MessageHelper.ShowInformation($"所有文件都已存在于列表中（{skippedCount:N0} 个文件）");
            }
        }

        /// <summary>
        /// Clear all items
        /// </summary>
        [RelayCommand]
        private void ClearItems()
        {
            _itemsCache.Clear();
        }

        private void RebuildCacheFromPaths(IEnumerable<string> paths)
        {
            var existingPaths = paths
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _itemsCache.Clear();

            if (existingPaths.Length == 0)
            {
                UpdateStatus();
                return;
            }

            AddFilesToList(existingPaths);
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
                    _itemsCache.Remove(item);
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
        /// Update rename preview based on pattern (with throttle)
        /// </summary>
        partial void OnRenamePatternChanged(string value)
        {
            // Clear cache only if pattern actually changed
                _cachedCompiledFunction = null;
            // Actual preview update handled by throttled observable in constructor
        }

        /// <summary>
        /// Update preview of renamed files based on template pattern
        /// Directly calculates all file names (no lazy evaluation) since compilation is fast
        /// </summary>
        private void UpdateRenamePreview()
        {
            if (_itemsCache.Count == 0)
            {
                UpdateStatus();
                return;
            }

            if (string.IsNullOrWhiteSpace(RenamePattern))
            {
                // If pattern is empty, keep original names unchanged
                // Clear cache for empty pattern
                _cachedCompiledFunction = null;
                
                foreach (var item in _itemsCache.Items)
                {
                    item.NewName = item.OriginalName;
                }
                UpdateStatus();
                return;
            }

            try
            {
                // Get or compile template function
                var compiledFunction = CompilePattern();
                if (compiledFunction == null)
                {
                    throw new Exception("模式串解析失败");
                }

                // Directly calculate all file names (no lazy evaluation)
                // Since compilation is fast, we can calculate all names immediately
                var itemsList = _itemsCache.Items.ToList();
                var totalCount = itemsList.Count;
                
                for (int i = 0; i < itemsList.Count; i++)
                {
                    var item = itemsList[i];
                    
                    // Calculate new name using helper method
                    var newName = CalculateNewNameForItem(item, i, totalCount, compiledFunction);
                    if (newName != null)
                    {
                        item.NewName = newName;
                        item._needsRecalculation = false;
                        item._cachedNewName = newName;
                    }
                    else
                    {
                        // On error, show error message
                        item.NewName = $"[执行错误]";
                        item._needsRecalculation = false;
                    }
                }
            }
            catch (ParseException ex)
            {
                // On parse error, show error message in preview for all items
                var errorMessage = $"解析错误: {ex.Message}";
                foreach (var item in _itemsCache.Items)
                {
                    item.NewName = $"[{errorMessage}]";
                }
            }
            catch (Exception ex)
            {
                // On other errors (evaluation errors, etc.), show generic error
                var errorMessage = $"执行错误: {ex.Message}";
                foreach (var item in _itemsCache.Items)
                {
                    item.NewName = $"[{errorMessage}]";
                }
            }
            
            // Update status after preview update
            UpdateStatus();
        }

        /// <summary>
        /// Calculate new name for a specific item (called when item becomes visible)
        /// </summary>
        public void CalculateItemNewName(FileRenameItem item, int index)
        {
            if (item == null || !item.NeedsRecalculation)
                return;

            if (string.IsNullOrWhiteSpace(RenamePattern))
            {
                item.NewName = item.OriginalName;
                item._needsRecalculation = false;
                return;
            }

            var compiledFunction = CompilePattern();
            if (compiledFunction == null)
                return;

            var newName = CalculateNewNameForItem(item, index, _itemsCache.Count, compiledFunction);
            if (newName != null)
            {
                item.NewName = newName;
                item._needsRecalculation = false;
                item._cachedNewName = newName;
            }
            else
            {
                // On error, keep current name or show error
                if (string.IsNullOrEmpty(item.NewName))
                {
                    item.NewName = item.OriginalName;
                }
                item._needsRecalculation = false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Start a rename operation (set progress state)
        /// </summary>
        private bool StartRenameOperation()
        {
            if (IsRenaming)
                return false;

            IsRenaming = true;
            ProgressValue = 0;
            ProgressText = "";
            return true;
        }

        /// <summary>
        /// End a rename operation (reset progress state)
        /// </summary>
        private void EndRenameOperation()
        {
            IsRenaming = false;
            ProgressValue = 0;
            ProgressText = "";
        }

        /// <summary>
        /// Compile the rename pattern into a function
        /// </summary>
        private Func<IEvaluationContext, string>? CompilePattern()
        {
            if (string.IsNullOrWhiteSpace(RenamePattern))
                return null;

            // Use cache if pattern hasn't changed
            if (_cachedCompiledFunction != null)
            {
                return _cachedCompiledFunction;
            }

            try
            {
                var templateNode = _parser.Parse(RenamePattern);
                var compiledFunction = _compiler.Compile(templateNode);
                
                // Cache the compiled function
                _cachedCompiledFunction = compiledFunction;
                
                return compiledFunction;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to compile pattern: {ex.Message}");
                _cachedCompiledFunction = null;
                return null;
            }
        }

        /// <summary>
        /// Calculate new name for a file item using compiled function
        /// </summary>
        private string? CalculateNewNameForItem(FileRenameItem item, int index, int totalCount, Func<IEvaluationContext, string> compiledFunction)
        {
            try
            {
                var extension = Path.GetExtension(item.OriginalName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(item.OriginalName);

                // Create evaluation context for this file
                var context = new EvaluationContext(
                    name: nameWithoutExt,
                    ext: extension.TrimStart('.'),
                    fullName: item.OriginalName,
                    fullPath: item.FullPath,
                    index: index,
                    totalCount: totalCount,
                    fileRenameItem: item
                );

                // Execute compiled function to get new name
                var newName = compiledFunction(context);

                // Auto-add extension if template doesn't include it
                newName = AutoAddExtension(newName, extension);

                // Validate the new name
                if (string.IsNullOrWhiteSpace(newName) || 
                    newName.StartsWith("[") || 
                    newName.Contains("解析错误") ||
                    newName.Contains("执行错误"))
                {
                    return null;
                }

                return newName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to calculate new name for {item.OriginalName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a progress reporter for rename operations
        /// </summary>
        private IProgress<(int current, int total, string fileName)> CreateProgressReporter(string actionText, double startPercent = 0, double rangePercent = 100)
        {
            return new Progress<(int current, int total, string fileName)>(report =>
            {
                // Use InvokeAsync to avoid blocking, and handle null dispatcher gracefully
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                {
                    dispatcher.InvokeAsync(() =>
                    {
                        if (report.total > 0)
                        {
                            ProgressValue = startPercent + (report.current * rangePercent) / report.total;
                            ProgressText = $"{actionText}: {report.current}/{report.total} - {report.fileName}";
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Show result message for rename operation
        /// </summary>
        private void ShowRenameResult(BatchRenameExecutor.RenameResult result, string successPrefix = "成功", string operationName = "重命名")
        {
            // Ensure we're on UI thread before showing messages
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.InvokeAsync(() => ShowRenameResult(result, successPrefix, operationName));
                return;
            }

            if (result.ErrorCount > 0)
            {
                // Build detailed error message
                var errorMessages = new List<string>();
                errorMessages.Add($"❌ {operationName}失败: {result.ErrorCount} 个文件\n");
                
                foreach (var error in result.Errors)
                {
                    errorMessages.Add($"  • {error}");
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

                var fullMessage = string.Join("\n", errorMessages);
                MessageHelper.ShowError(fullMessage);
            }
            else
            {
                // No errors, show success summary
                var messageParts = new List<string>();
                if (result.SuccessCount > 0)
                {
                    messageParts.Add($"{successPrefix}: {result.SuccessCount} 个文件");
                }
                if (result.SkippedCount > 0)
                {
                    messageParts.Add($"跳过: {result.SkippedCount} 个文件");
                }

                var message = string.Join("，", messageParts);
                if (string.IsNullOrEmpty(message))
                {
                    message = $"没有执行任何{operationName}操作";
                }

                if (result.SuccessCount > 0)
                {
                    MessageHelper.ShowSuccess(message);
                }
                else
                {
                    MessageHelper.ShowInformation(message);
                }
            }
        }

        /// <summary>
        /// Check if a rename operation was successful
        /// </summary>
        private bool WasRenameSuccessful(string oldFullPath, string newFullPath)
        {
            // If paths are the same, no rename happened (should not be recorded in history)
            if (string.Equals(oldFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // If new file exists, the rename is successful
            return File.Exists(newFullPath);
        }

        /// <summary>
        /// Record successful renames for undo functionality and return list of successful operations
        /// </summary>
        private List<SingleRenameOperation> RecordRenameHistory(
            List<BatchRenameExecutor.RenameOperation> operations,
            BatchRenameExecutor.RenameResult result)
        {
            var historyEntry = new RenameHistoryEntry
            {
                Timestamp = DateTime.Now,
                Operations = new List<SingleRenameOperation>()
            };

            // Record only successfully renamed items
            foreach (var operation in operations)
            {
                var newFullPath = Path.Combine(operation.Directory, operation.NewName);
                var oldFullPath = operation.OriginalPath;

                if (WasRenameSuccessful(oldFullPath, newFullPath))
                {
                    // Record for undo
                    var historyOp = new SingleRenameOperation(oldFullPath, newFullPath);
                    historyEntry.Operations.Add(historyOp);
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

            return historyEntry.Operations;
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
        /// Execute rename operation using BatchRenameService
        /// </summary>
        [RelayCommand]
        private async Task ExecuteRenameAsync()
        {
            if (Items.Count == 0)
                return;

            // Prevent multiple simultaneous rename operations
            if (IsRenaming)
                return;

            IsRenaming = true;
            ProgressValue = 0;
            ProgressText = "";

            try
            {
                // Validate pattern before proceeding
                if (string.IsNullOrWhiteSpace(RenamePattern))
                {
                    MessageHelper.ShowWarning("重命名模式不能为空");
                    return;
                }

                // Use _itemsCache.Items directly and create a snapshot to avoid collection changes during iteration
                var itemsList = _itemsCache.Items.ToList();
                var totalCount = itemsList.Count;
                
                if (totalCount == 0)
                {
                    MessageHelper.ShowWarning("没有文件需要重命名");
                    return;
                }

                // Get file paths
                var filePaths = itemsList.Select(item => item.FullPath).ToList();

                // Create progress reporter for rename phase
                var progress = CreateProgressReporter("正在重命名", startPercent: 0.0, rangePercent: 100.0);

                // Create service and execute rename
                var service = new BatchRenameService();
                BatchRenameExecutor.RenameResult result;
                try
                {
                    result = await Task.Run(() => service.RenameFiles(filePaths, RenamePattern, progress)).ConfigureAwait(true);
                }
                catch (BatchRenameExecutor.RenameValidationException ex)
                {
                    MessageHelper.ShowError($"重命名失败：{ex.Message}");
                    return;
                }

                // Get operations from result for history recording
                var operations = result.Operations;

                // Ensure we're back on UI thread before updating UI
                // Show result message
                ShowRenameResult(result, successPrefix: "成功", operationName: "重命名");

                // Record successful renames for undo functionality and get list of successful operations
                var successfulOperations = RecordRenameHistory(operations, result);

                // 批量更新：先收集所有要移除和添加的项目，然后一次性更新
                if (successfulOperations.Count > 0)
                {
                    // 创建字典用于快速查找旧项（使用 FullPath 作为 key）
                    var itemsByPath = _itemsCache.Items.ToDictionary(i => i.FullPath, StringComparer.OrdinalIgnoreCase);
                    
                    // 收集要移除的旧项和要添加的新项
                    var itemsToRemove = new List<FileRenameItem>();
                    var itemsToAdd = new List<FileRenameItem>();
                    
                    foreach (var historyOp in successfulOperations)
                    {
                        // 查找并收集要移除的旧项
                        if (itemsByPath.TryGetValue(historyOp.OriginalFullPath, out var oldItem))
                        {
                            itemsToRemove.Add(oldItem);
                        }

                        // 收集要添加的新项
                        if (File.Exists(historyOp.NewFullPath))
                        {
                            var newItem = new FileRenameItem(historyOp.NewFullPath)
                            {
                                NewName = historyOp.NewName
                            };
                            itemsToAdd.Add(newItem);
                        }
                    }

                    // 批量移除旧项
                    if (itemsToRemove.Count > 0)
                    {
                        _itemsCache.Remove(itemsToRemove);
                    }

                    // 批量添加新项
                    if (itemsToAdd.Count > 0)
                    {
                        _itemsCache.AddOrUpdate(itemsToAdd);
                    }
                }

                // Refresh the preview for remaining items
                UpdateRenamePreview();
            }
            finally
            {
                EndRenameOperation();
            }
        }

        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo => RenameHistory.Count > 0;

        /// <summary>
        /// Undo the last rename operation
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private async Task UndoRenameAsync()
        {
            if (RenameHistory.Count == 0)
                return;

            if (!StartRenameOperation())
                return;

            try
            {
                // Get the last history entry
                var lastEntry = RenameHistory[RenameHistory.Count - 1];

                // Create reverse operations (rename back to original names)
                var executor = new BatchRenameExecutor();
                var undoOperations = lastEntry.Operations.Select(op => 
                    new BatchRenameExecutor.RenameOperation(op.NewFullPath, op.OriginalName)).ToList();

                if (undoOperations.Count == 0)
                {
                    MessageHelper.ShowInformation("没有可撤销的操作");
                    return;
                }

                // Create progress reporter
                var progress = CreateProgressReporter("正在撤销");

                // Execute undo asynchronously
                BatchRenameExecutor.RenameResult result;
                try
                {
                    result = await Task.Run(() => executor.Execute(undoOperations, progress)).ConfigureAwait(true);
                }
                catch (BatchRenameExecutor.RenameValidationException ex)
                {
                    MessageHelper.ShowError($"撤销失败：{ex.Message}");
                    return;
                }

                // Remove from history
                RenameHistory.RemoveAt(RenameHistory.Count - 1);
                OnPropertyChanged(nameof(CanUndo));

                // Show result
                ShowRenameResult(result, successPrefix: "已撤销", operationName: "撤销");

                // 批量更新：先收集所有要移除和添加的项目，然后一次性更新
                var successfulUndoOps = lastEntry.Operations
                    .Where(op => WasRenameSuccessful(op.NewFullPath, op.OriginalFullPath))
                    .ToList();
                
                if (successfulUndoOps.Count > 0)
                {
                    // 创建字典用于快速查找项（使用 FullPath 作为 key）
                    var itemsByPath = _itemsCache.Items.ToDictionary(i => i.FullPath, StringComparer.OrdinalIgnoreCase);
                    
                    // 收集要移除的项和要添加的项
                    var itemsToRemove = new List<FileRenameItem>();
                    var itemsToAdd = new List<FileRenameItem>();
                    
                    foreach (var historyOp in successfulUndoOps)
                    {
                        // 查找并收集要移除的项（新路径）
                        if (itemsByPath.TryGetValue(historyOp.NewFullPath, out var newItem))
                        {
                            itemsToRemove.Add(newItem);
                        }

                        // 收集要添加的项（原始路径）
                        if (File.Exists(historyOp.OriginalFullPath))
                        {
                            var restoredItem = new FileRenameItem(historyOp.OriginalFullPath)
                            {
                                NewName = historyOp.OriginalName
                            };
                            itemsToAdd.Add(restoredItem);
                        }
                    }

                    // 批量移除项
                    if (itemsToRemove.Count > 0)
                    {
                        _itemsCache.Remove(itemsToRemove);
                    }

                    // 批量添加项
                    if (itemsToAdd.Count > 0)
                    {
                        _itemsCache.AddOrUpdate(itemsToAdd);
                    }
                }
                
                UpdateRenamePreview();
            }
            finally
            {
                EndRenameOperation();
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a file item for renaming
    /// </summary>
    public partial class FileRenameItem : ObservableObject
    {
        private Lazy<Template.Evaluator.ImageInfo>? _imageInfo;
        internal bool _needsRecalculation = false;
        internal string? _cachedNewName = null;

        public FileRenameItem(string fullPath)
        {
            FullPath = fullPath;
            NewName = Path.GetFileName(fullPath);
        }

        public string OriginalName => Path.GetFileName(FullPath);

        [ObservableProperty]
        private string _newName = "";

        public string FullPath { get; }

        /// <summary>
        /// Mark this item as needing recalculation
        /// </summary>
        public void MarkForRecalculation()
        {
            _needsRecalculation = true;
            _cachedNewName = null; // Clear cache when marked for recalculation
        }

        /// <summary>
        /// Check if this item needs recalculation
        /// </summary>
        public bool NeedsRecalculation => _needsRecalculation;


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
    }

    /// <summary>
    /// Represents a single rename operation in history
    /// </summary>
    public class SingleRenameOperation
    {
        public string OriginalFullPath { get; }
        public string NewFullPath { get; }

        public SingleRenameOperation(string originalFullPath, string newFullPath)
        {
            OriginalFullPath = originalFullPath;
            NewFullPath = newFullPath;
        }

        // Computed properties from FullPath
        public string Directory => Path.GetDirectoryName(OriginalFullPath) ?? "";
        public string OriginalName => Path.GetFileName(OriginalFullPath);
        public string NewName => Path.GetFileName(NewFullPath);
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
