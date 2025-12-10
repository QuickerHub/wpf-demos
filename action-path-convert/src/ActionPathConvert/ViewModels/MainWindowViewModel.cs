using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using ActionPathConvert.Models;
using ActionPathConvert.Models.Config;
using ActionPathConvert.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ActionPathConvert.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly PathConvertService _pathConvertService;
        private readonly ConfigService _configService;
        private readonly PathConvertConfig _config;

        public MainWindowViewModel()
        {
            _pathConvertService = new PathConvertService();
            _configService = new ConfigService();
            _config = _configService.GetConfig<PathConvertConfig>();
            
            // Load config values
            SearchDirectory = _config.SearchDirectory;
            AudioExtensions = _config.AudioExtensions;
            PreferredExtension = _config.PreferredExtension;
            UseRelativePath = _config.UseRelativePath;
            
            // Save config when properties change
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchDirectory))
                {
                    _config.SearchDirectory = SearchDirectory;
                }
                else if (e.PropertyName == nameof(AudioExtensions))
                {
                    _config.AudioExtensions = AudioExtensions;
                }
                else if (e.PropertyName == nameof(PreferredExtension))
                {
                    _config.PreferredExtension = PreferredExtension;
                }
                else if (e.PropertyName == nameof(UseRelativePath))
                {
                    _config.UseRelativePath = UseRelativePath;
                }
            };
            
            // Listen to OutputFiles collection changes to update preview
            OutputFiles.CollectionChanged += (s, e) =>
            {
                if (OutputFiles.Count > 0)
                {
                    ProcessedFileContent = string.Join(Environment.NewLine, OutputFiles);
                }
                else
                {
                    ProcessedFileContent = "";
                }
            };

            // Listen to InputFiles collection changes to update command state and preview
            InputFiles.CollectionChanged += (s, e) =>
            {
                ProcessFilesCommand.NotifyCanExecuteChanged();
                UpdatePreview();
            };

            // Subscribe to property changes to update command state and preview
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchDirectory) || 
                    e.PropertyName == nameof(IsProcessing))
                {
                    ProcessFilesCommand.NotifyCanExecuteChanged();
                }

                // Update preview when configuration changes
                if (e.PropertyName == nameof(SearchDirectory) ||
                    e.PropertyName == nameof(AudioExtensions) ||
                    e.PropertyName == nameof(PreferredExtension) ||
                    e.PropertyName == nameof(UseRelativePath) ||
                    e.PropertyName == nameof(SelectedFileContent))
                {
                    UpdatePreview();
                }
            };
        }

        [ObservableProperty]
        public partial string Title { get; set; } = "路径转换";

        [ObservableProperty]
        public partial ObservableCollection<string> InputFiles { get; set; } = new ObservableCollection<string>();

        [ObservableProperty]
        public partial string SearchDirectory { get; set; } = "";

        [ObservableProperty]
        public partial string AudioExtensions { get; set; } = "*.mp3,*.flac,*.mp4";

        [ObservableProperty]
        public partial string PreferredExtension { get; set; } = ".mp3";

        [ObservableProperty]
        public partial bool UseRelativePath { get; set; } = true;

        [ObservableProperty]
        public partial ObservableCollection<string> OutputFiles { get; set; } = new ObservableCollection<string>();

        [ObservableProperty]
        public partial ObservableCollection<ProcessResultViewModel> ProcessResults { get; set; } = new ObservableCollection<ProcessResultViewModel>();

        [ObservableProperty]
        public partial ObservableCollection<string> NotFoundFiles { get; set; } = new ObservableCollection<string>();

        [ObservableProperty]
        public partial string SelectedFileContent { get; set; } = "";

        [ObservableProperty]
        public partial string ProcessedFileContent { get; set; } = "";

        [ObservableProperty]
        public partial bool IsProcessing { get; set; } = false;

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = "就绪";

        [ObservableProperty]
        public partial ProcessResultViewModel? SelectedProcessResult { get; set; }

        /// <summary>
        /// Clear all input files
        /// </summary>
        [RelayCommand]
        private void ClearFiles()
        {
            InputFiles.Clear();
        }

        /// <summary>
        /// Remove successfully processed files from input list and process results
        /// </summary>
        [RelayCommand]
        private void RemoveProcessedFiles()
        {
            if (ProcessResults.Count == 0)
                return;

            // Get all process results that have been successfully processed (have M3U file generated)
            var processedResults = ProcessResults
                .Where(r => !string.IsNullOrEmpty(r.M3uFilePath) && !string.IsNullOrEmpty(r.InputFileName))
                .ToList();

            if (processedResults.Count == 0)
                return;

            // Get all input files that have been successfully processed
            var processedInputFiles = processedResults
                .Select(r => r.InputFileName)
                .Distinct()
                .ToList();

            // Remove processed files from InputFiles
            foreach (var processedFile in processedInputFiles)
            {
                InputFiles.Remove(processedFile);
            }

            // Remove processed results from ProcessResults
            foreach (var result in processedResults)
            {
                ProcessResults.Remove(result);
            }

            // Clear selection if the selected result was removed
            if (SelectedProcessResult != null && !ProcessResults.Contains(SelectedProcessResult))
            {
                SelectedProcessResult = null;
            }
        }

        /// <summary>
        /// Paste files from clipboard
        /// </summary>
        [RelayCommand]
        private void PasteFiles()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText())
                {
                    StatusMessage = "剪贴板中没有文本内容";
                    MessageHelper.ShowWarning("剪贴板中没有文本内容");
                    return;
                }

                var clipboardText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    StatusMessage = "剪贴板内容为空";
                    MessageHelper.ShowWarning("剪贴板内容为空");
                    return;
                }

                // Split by newlines and process each line
                var lines = clipboardText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                int addedCount = 0;
                int skippedCount = 0;

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
                        if (!InputFiles.Contains(trimmedLine))
                        {
                            InputFiles.Add(trimmedLine);
                            addedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    var message = $"已从剪贴板添加 {addedCount} 个文件" + (skippedCount > 0 ? $"，跳过 {skippedCount} 个" : "");
                    StatusMessage = message;
                    MessageHelper.ShowSuccess(message);
                }
                else
                {
                    var message = skippedCount > 0 ? "没有找到有效的文件路径" : "剪贴板中没有有效的文件路径";
                    StatusMessage = message;
                    MessageHelper.ShowWarning(message);
                }
            }
            catch (Exception ex)
            {
                var message = $"从剪贴板粘贴文件失败: {ex.Message}";
                StatusMessage = message;
                MessageHelper.ShowError(message);
            }
        }

        /// <summary>
        /// Add files using file dialog
        /// </summary>
        [RelayCommand]
        private void AddFiles()
        {
            var fileNames = FileDialog.ShowOpenFileDialog(
                title: "选择文件",
                filter: "播放列表文件 (*.m3u;*.m3u8)|*.m3u;*.m3u8|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                filterIndex: 1);

            if (fileNames != null)
            {
                foreach (var file in fileNames)
                {
                    if (!InputFiles.Contains(file))
                    {
                        InputFiles.Add(file);
                    }
                }
            }
        }

        /// <summary>
        /// Remove selected files
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRemoveFiles))]
        private void RemoveFiles(object? selectedItems)
        {
            if (selectedItems is System.Collections.IList items)
            {
                var filesToRemove = items.Cast<string>().ToList();
                foreach (var file in filesToRemove)
                {
                    InputFiles.Remove(file);
                }
            }
        }

        private bool CanRemoveFiles(object? selectedItems)
        {
            return selectedItems is System.Collections.IList items && items.Count > 0;
        }

        /// <summary>
        /// Process all files
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanProcess))]
        private void ProcessFiles()
        {
            if (InputFiles.Count == 0 || string.IsNullOrWhiteSpace(SearchDirectory) || !Directory.Exists(SearchDirectory))
            {
                return;
            }

            IsProcessing = true;
            OutputFiles.Clear();
            NotFoundFiles.Clear();
            ProcessResults.Clear();

            try
            {
                int processedCount = 0;
                int savedCount = 0;
                int errorCount = 0;

                // Process each input file separately
                foreach (var inputFile in InputFiles)
                {
                    if (!File.Exists(inputFile))
                        continue;

                    try
                    {
                        StatusMessage = $"正在处理: {Path.GetFileName(inputFile)}...";
                        MessageHelper.ShowInformation($"正在处理: {Path.GetFileName(inputFile)}...");

                        // Read and extract paths from input file
                        var content = File.ReadAllText(inputFile);
                        var inputPaths = ExtractFilePaths(content, inputFile);

                        if (inputPaths.Count == 0)
                        {
                            StatusMessage = $"跳过空文件: {Path.GetFileName(inputFile)}";
                            MessageHelper.ShowWarning($"跳过空文件: {Path.GetFileName(inputFile)}");
                            continue;
                        }

                // Process paths for this input file - use SearchDirectory as removePathPrefix if UseRelativePath is enabled
                var removePathPrefix = UseRelativePath ? SearchDirectory : string.Empty;
                var result = _pathConvertService.ProcessFilePaths(
                    inputPaths,
                    SearchDirectory,
                    AudioExtensions,
                    PreferredExtension,
                    removePathPrefix);

                        // Add to output collections (for display)
                        foreach (var file in result.OutputFiles)
                        {
                            if (!OutputFiles.Contains(file))
                            {
                                OutputFiles.Add(file);
                            }
                        }

                        // Save output M3U file for this input file
                        if (result.OutputFiles.Count > 0)
                        {
                            var m3uFilePath = SaveOutputM3uFile(inputFile, result.OutputFiles);
                            if (!string.IsNullOrEmpty(m3uFilePath))
                            {
                                // Create ProcessResultViewModel with M3U file path and its not found files
                                var processResult = new ProcessResultViewModel
                                {
                                    M3uFilePath = m3uFilePath,
                                    MatchedFilesCount = result.OutputFiles.Count,
                                    InputFileName = inputFile
                                };
                                processResult.UpdateNotFoundFiles(result.NotFoundFiles);
                                ProcessResults.Add(processResult);
                                savedCount++;
                            }
                        }
                        else if (result.NotFoundFiles.Count > 0)
                        {
                            // Even if no output files, still record not found files
                            var processResult = new ProcessResultViewModel
                            {
                                M3uFilePath = "", // No M3U file generated
                                MatchedFilesCount = 0,
                                InputFileName = inputFile
                            };
                            processResult.UpdateNotFoundFiles(result.NotFoundFiles);
                            ProcessResults.Add(processResult);
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        // Log error but continue processing other files
                        System.Diagnostics.Debug.WriteLine($"Failed to process {inputFile}: {ex.Message}");
                        var errorMessage = $"处理失败: {Path.GetFileName(inputFile)} - {ex.Message}";
                        StatusMessage = errorMessage;
                        MessageHelper.ShowError(errorMessage);
                    }
                }

                // Update processed content for preview (will be updated by CollectionChanged event)
                // But also update here to ensure it's set even if collection is empty
                UpdateProcessedContent();

                // Update selected process result
                // If there's a currently selected result, refresh it
                if (SelectedProcessResult != null && ProcessResults.Contains(SelectedProcessResult))
                {
                    // Keep the selection
                    var currentResult = SelectedProcessResult;
                    SelectedProcessResult = null;
                    // Use Dispatcher to ensure UI updates properly
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedProcessResult = currentResult;
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                }
                // If no result is selected but we have results, select the first one
                else if (ProcessResults.Count > 0 && SelectedProcessResult == null)
                {
                    var firstResult = ProcessResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.M3uFilePath)) ?? ProcessResults[0];
                    // Use Dispatcher to ensure UI updates properly
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedProcessResult = firstResult;
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                }

                // Show completion message
                if (errorCount > 0)
                {
                    var message = $"处理完成: 成功 {processedCount} 个，保存 {savedCount} 个文件，失败 {errorCount} 个";
                    StatusMessage = message;
                    MessageHelper.ShowWarning(message);
                }
                else
                {
                    var message = $"处理完成: 成功处理 {processedCount} 个文件，已保存 {savedCount} 个 M3U 文件";
                    StatusMessage = message;
                    MessageHelper.ShowSuccess(message);
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanProcess()
        {
            return !IsProcessing && InputFiles.Count > 0 && !string.IsNullOrWhiteSpace(SearchDirectory) && Directory.Exists(SearchDirectory);
        }

        /// <summary>
        /// Select search directory
        /// </summary>
        [RelayCommand]
        private void SelectSearchDirectory()
        {
            var selectedPath = FileDialog.ShowFolderBrowserDialog(
                description: "选择目标搜索目录",
                selectedPath: SearchDirectory,
                showNewFolderButton: true);

            if (selectedPath != null)
            {
                SearchDirectory = selectedPath;
            }
        }

        /// <summary>
        /// Preview selected file
        /// </summary>
        [RelayCommand]
        private void PreviewFile(object? selectedItem)
        {
            if (selectedItem is string filePath && File.Exists(filePath))
            {
                SelectedFileContent = File.ReadAllText(filePath);
                UpdatePreview();
            }
        }

        /// <summary>
        /// Update preview content based on current selection and configuration
        /// </summary>
        private void UpdatePreview()
        {
            if (string.IsNullOrEmpty(SelectedFileContent))
            {
                ProcessedFileContent = "";
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchDirectory) || !Directory.Exists(SearchDirectory))
            {
                ProcessedFileContent = "";
                return;
            }

            try
            {
                // Extract paths from selected file content
                // We need to find which input file is currently selected
                string? currentInputFile = null;
                foreach (var inputFile in InputFiles)
                {
                    if (File.Exists(inputFile))
                    {
                        var content = File.ReadAllText(inputFile);
                        if (content == SelectedFileContent)
                        {
                            currentInputFile = inputFile;
                            break;
                        }
                    }
                }

                if (currentInputFile == null)
                    return;

                var inputPaths = ExtractFilePaths(SelectedFileContent, currentInputFile);

                if (inputPaths.Count == 0)
                {
                    ProcessedFileContent = "";
                    return;
                }

                // Process paths - use SearchDirectory as removePathPrefix if UseRelativePath is enabled
                var removePathPrefix = UseRelativePath ? SearchDirectory : string.Empty;
                var result = _pathConvertService.ProcessFilePaths(
                    inputPaths,
                    SearchDirectory,
                    AudioExtensions,
                    PreferredExtension,
                    removePathPrefix);

                // Update preview content
                ProcessedFileContent = string.Join(Environment.NewLine, result.OutputFiles);
            }
            catch
            {
                ProcessedFileContent = "";
            }
        }

        /// <summary>
        /// Update processed content for preview
        /// </summary>
        private void UpdateProcessedContent()
        {
            if (OutputFiles.Count > 0)
            {
                ProcessedFileContent = string.Join(Environment.NewLine, OutputFiles);
            }
            else
            {
                ProcessedFileContent = "";
            }
        }

        /// <summary>
        /// Update preview after processing
        /// </summary>
        partial void OnOutputFilesChanged(ObservableCollection<string> value)
        {
            // This is called when the entire collection is replaced, not when items are added
            UpdateProcessedContent();
        }

        /// <summary>
        /// Save output M3U file for a specific input file
        /// </summary>
        /// <param name="inputFile">Input file path</param>
        /// <param name="outputPaths">Output file paths for this input file</param>
        /// <returns>Path to the saved M3U file</returns>
        private string SaveOutputM3uFile(string inputFile, List<string> outputPaths)
        {
            if (outputPaths.Count == 0)
                return string.Empty;

            try
            {
                var inputDirectory = Path.GetDirectoryName(inputFile);
                if (string.IsNullOrEmpty(inputDirectory))
                    return string.Empty;

                var inputFileName = Path.GetFileNameWithoutExtension(inputFile);
                var outputM3uPath = Path.Combine(inputDirectory, $"{inputFileName}.m3u");

                // Create simple M3U format content (only file paths, no metadata)
                var m3uContent = string.Join(Environment.NewLine, outputPaths);

                File.WriteAllText(outputM3uPath, m3uContent, System.Text.Encoding.UTF8);
                
                // Update status message
                StatusMessage = $"已保存: {Path.GetFileName(outputM3uPath)}";
                MessageHelper.ShowSuccess($"已保存: {Path.GetFileName(outputM3uPath)}");
                
                return outputM3uPath;
            }
            catch (Exception ex)
            {
                // Log error but continue processing other files
                System.Diagnostics.Debug.WriteLine($"Failed to save M3U file for {inputFile}: {ex.Message}");
                throw; // Re-throw to be caught by caller
            }
        }

        /// <summary>
        /// Extract file paths from content, filtering out M3U8/M3U metadata tags
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="sourceFile">Source file path (for resolving relative paths)</param>
        /// <returns>List of extracted file paths</returns>
        private List<string> ExtractFilePaths(string content, string sourceFile)
        {
            var paths = new List<string>();
            var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var sourceDirectory = Path.GetDirectoryName(sourceFile) ?? "";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Skip all M3U8/M3U metadata tags (lines starting with #)
                // Examples: #EXTM3U, #EXTINF, #EXT-X-VERSION, #EXT-X-STREAM-INF, etc.
                if (trimmedLine.StartsWith("#"))
                    continue;

                // Skip lines that look like URLs (http://, https://, ftp://, etc.)
                // These are network streams, not local file paths
                if (trimmedLine.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Remove quotes if present (some playlists may quote paths)
                trimmedLine = trimmedLine.Trim('"', '\'');

                // Skip if still empty after trimming quotes
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // URL decode if needed (some playlists may have URL-encoded paths)
                // Example: "C%3A%5CMusic%5Csong.mp3" -> "C:\Music\song.mp3"
                try
                {
                    var decoded = Uri.UnescapeDataString(trimmedLine);
                    // Only use decoded version if it's different and looks like a valid path
                    if (decoded != trimmedLine && !decoded.Contains("://"))
                    {
                        trimmedLine = decoded;
                    }
                }
                catch
                {
                    // If URL decoding fails, use original string
                }

                // Resolve relative paths to absolute paths if needed
                string finalPath = trimmedLine;
                if (!Path.IsPathRooted(trimmedLine) && !string.IsNullOrEmpty(sourceDirectory))
                {
                    try
                    {
                        finalPath = Path.Combine(sourceDirectory, trimmedLine);
                        // Normalize path separators and resolve .. and .
                        finalPath = Path.GetFullPath(finalPath);
                    }
                    catch
                    {
                        // If path resolution fails, use original trimmed line
                        finalPath = trimmedLine;
                    }
                }

                // Only add if it looks like a valid file path (contains at least one dot for extension or is a directory)
                if (!string.IsNullOrWhiteSpace(finalPath))
                {
                    paths.Add(finalPath);
                }
            }

            return paths;
        }
    }
}
