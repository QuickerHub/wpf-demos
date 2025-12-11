using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        public partial string AudioExtensions { get; set; } = "*.mp3,*.flac,*.mp4,*.wav,*.m4a,*.aac,*.ogg,*.wma";

        /// <summary>
        /// Default audio extensions value
        /// </summary>
        private const string DefaultAudioExtensions = "*.mp3,*.flac,*.mp4,*.wav,*.m4a,*.aac,*.ogg,*.wma";

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
        public partial string SelectedFilePath { get; set; } = "";

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
                
                // Filter playlist files only (asx, dpl, m3u, m3u8, pls, wpl)
                var playlistExtensions = new[] { ".asx", ".dpl", ".m3u", ".m3u8", ".pls", ".wpl" };
                var playlistFiles = filesToAdd
                    .Where(f => playlistExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();
                
                int addedCount = 0;
                int skippedCount = 0;
                
                foreach (var file in playlistFiles)
                {
                    if (!InputFiles.Contains(file))
                    {
                        InputFiles.Add(file);
                        addedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                
                // Report non-playlist files that were skipped
                var nonPlaylistCount = filesToAdd.Count - playlistFiles.Count;
                
                if (addedCount > 0)
                {
                    var message = $"已从剪贴板添加 {addedCount} 个播放列表文件";
                    if (skippedCount > 0)
                    {
                        message += $"，跳过 {skippedCount} 个重复文件";
                    }
                    if (nonPlaylistCount > 0)
                    {
                        message += $"，忽略 {nonPlaylistCount} 个非播放列表文件";
                    }
                    StatusMessage = message;
                    MessageHelper.ShowSuccess(message);
                }
                else
                {
                    var message = filesToAdd.Count > 0 
                        ? $"剪贴板中有 {filesToAdd.Count} 个文件，但没有播放列表文件（支持格式：asx, dpl, m3u, m3u8, pls, wpl）" 
                        : "剪贴板中没有找到有效的文件路径";
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
                filter: "播放列表文件 (*.asx;*.dpl;*.m3u;*.m3u8;*.pls;*.wpl)|*.asx;*.dpl;*.m3u;*.m3u8;*.pls;*.wpl|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
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
        /// Process all files (async)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanProcess))]
        private async Task ProcessFiles()
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
                // Run processing in background thread to avoid blocking UI
                await Task.Run(async () =>
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
                            // Update status on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                StatusMessage = $"正在处理: {Path.GetFileName(inputFile)}...";
                            });
                            MessageHelper.ShowInformation($"正在处理: {Path.GetFileName(inputFile)}...");

                            // Read and extract paths from input file (run in background)
                            var content = await Task.Run(() => PlaylistFileHelper.ReadPlaylistFile(inputFile));
                            var inputPaths = await Task.Run(() => PlaylistFileHelper.ExtractFilePaths(content, inputFile));

                            if (inputPaths.Count == 0)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    StatusMessage = $"跳过空文件: {Path.GetFileName(inputFile)}";
                                });
                                MessageHelper.ShowWarning($"跳过空文件: {Path.GetFileName(inputFile)}");
                                continue;
                            }

                            // Process paths for this input file - use SearchDirectory as removePathPrefix if UseRelativePath is enabled
                            var removePathPrefix = UseRelativePath ? SearchDirectory : string.Empty;
                            var result = await Task.Run(() => _pathConvertService.ProcessFilePaths(
                                inputPaths,
                                SearchDirectory,
                                AudioExtensions,
                                PreferredExtension,
                                removePathPrefix));

                            // Update UI collections on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                // Add to output collections (for display)
                                foreach (var file in result.OutputFiles)
                                {
                                    if (!OutputFiles.Contains(file))
                                    {
                                        OutputFiles.Add(file);
                                    }
                                }
                            });

                            // Save output M3U file for this input file (run in background)
                            if (result.OutputFiles.Count > 0)
                            {
                                var m3uFilePath = await Task.Run(() => PlaylistFileHelper.SaveOutputM3uFile(inputFile, result.OutputFiles));
                                if (!string.IsNullOrEmpty(m3uFilePath))
                                {
                                    // Create ProcessResultViewModel with M3U file path and its not found files
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        var processResult = new ProcessResultViewModel
                                        {
                                            M3uFilePath = m3uFilePath,
                                            MatchedFilesCount = result.OutputFiles.Count,
                                            InputFileName = inputFile
                                        };
                                        processResult.UpdateNotFoundFiles(result.NotFoundFiles);
                                        ProcessResults.Add(processResult);
                                        
                                        // Update status message
                                        StatusMessage = $"已保存: {Path.GetFileName(m3uFilePath)}";
                                        MessageHelper.ShowSuccess($"已保存: {Path.GetFileName(m3uFilePath)}");
                                    });
                                    savedCount++;
                                }
                            }
                            else if (result.NotFoundFiles.Count > 0)
                            {
                                // Even if no output files, still record not found files
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    var processResult = new ProcessResultViewModel
                                    {
                                        M3uFilePath = "", // No M3U file generated
                                        MatchedFilesCount = 0,
                                        InputFileName = inputFile
                                    };
                                    processResult.UpdateNotFoundFiles(result.NotFoundFiles);
                                    ProcessResults.Add(processResult);
                                });
                            }

                            processedCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            // Log error but continue processing other files
                            System.Diagnostics.Debug.WriteLine($"Failed to process {inputFile}: {ex.Message}");
                            var errorMessage = $"处理失败: {Path.GetFileName(inputFile)} - {ex.Message}";
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                StatusMessage = errorMessage;
                            });
                            MessageHelper.ShowError(errorMessage);
                        }
                    }

                    // Update processed content for preview on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateProcessedContent();

                        // Update selected process result
                        // If there's a currently selected result, refresh it
                        if (SelectedProcessResult != null && ProcessResults.Contains(SelectedProcessResult))
                        {
                            // Keep the selection
                            var currentResult = SelectedProcessResult;
                            SelectedProcessResult = null;
                            SelectedProcessResult = currentResult;
                        }
                        // If no result is selected but we have results, select the first one
                        else if (ProcessResults.Count > 0 && SelectedProcessResult == null)
                        {
                            var firstResult = ProcessResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.M3uFilePath)) ?? ProcessResults[0];
                            SelectedProcessResult = firstResult;
                        }
                    });

                    // Show completion message on UI thread
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
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
                    });
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var message = $"处理文件时发生错误: {ex.Message}";
                    StatusMessage = message;
                    MessageHelper.ShowError(message);
                });
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
        /// Reset audio extensions to default value
        /// </summary>
        [RelayCommand]
        private void ResetAudioExtensions()
        {
            AudioExtensions = DefaultAudioExtensions;
        }

        /// <summary>
        /// Preview selected file
        /// </summary>
        [RelayCommand]
        private void PreviewFile(object? selectedItem)
        {
            if (selectedItem is string filePath && File.Exists(filePath))
            {
                SelectedFilePath = filePath;
                SelectedFileContent = PlaylistFileHelper.ReadPlaylistFile(filePath);
                UpdatePreview();
            }
        }

        /// <summary>
        /// Update preview content based on current selection and configuration
        /// </summary>
        public void UpdatePreview()
        {
            // Use SelectedFilePath if available, otherwise try to find from SelectedFileContent
            string? currentInputFile = SelectedFilePath;
            
            if (string.IsNullOrEmpty(currentInputFile) || !File.Exists(currentInputFile))
            {
                // Fallback: try to find file by content comparison
                foreach (var inputFile in InputFiles)
                {
                    if (File.Exists(inputFile))
                    {
                        var content = PlaylistFileHelper.ReadPlaylistFile(inputFile);
                        if (content == SelectedFileContent)
                        {
                            currentInputFile = inputFile;
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(currentInputFile) || !File.Exists(currentInputFile))
            {
                ProcessedFileContent = "";
                return;
            }

            // Read current content from file (in case it was edited)
            var fileContent = PlaylistFileHelper.ReadPlaylistFile(currentInputFile);
            if (string.IsNullOrEmpty(fileContent))
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
                // Extract paths from file content
                var inputPaths = PlaylistFileHelper.ExtractFilePaths(fileContent, currentInputFile);

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

    }
}
