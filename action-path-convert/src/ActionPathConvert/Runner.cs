using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ActionPathConvert.Models;
using ActionPathConvert.Models.Config;
using ActionPathConvert.Services;
using ActionPathConvert.ViewModels;

namespace ActionPathConvert
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
        public static void ShowMainWindow()
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
            });
        }

        /// <summary>
        /// Process a single playlist file using stored configuration
        /// </summary>
        /// <param name="filePath">Path to the playlist file to process</param>
        /// <param name="targetDirectory">Target directory to search for files (overrides config if provided)</param>
        /// <returns>Processing result</returns>
        public static PathConvertResult ProcessFile(string filePath, string? targetDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            var fileList = new List<string> { filePath };
            return ProcessFiles(fileList, targetDirectory);
        }

        /// <summary>
        /// Process multiple playlist files using stored configuration
        /// </summary>
        /// <param name="filePaths">List of playlist file paths to process</param>
        /// <param name="targetDirectory">Target directory to search for files (overrides config if provided)</param>
        /// <returns>Processing result containing all processed files</returns>
        public static PathConvertResult ProcessFiles(List<string> filePaths, string? targetDirectory = null)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                throw new ArgumentException("File paths list cannot be null or empty", nameof(filePaths));
            }

            // Load configuration from storage
            var configService = new ConfigService();
            var config = configService.GetConfig<PathConvertConfig>();

            // Use provided target directory or fall back to config
            var searchDirectory = !string.IsNullOrWhiteSpace(targetDirectory) 
                ? targetDirectory 
                : config.SearchDirectory;

            if (string.IsNullOrWhiteSpace(searchDirectory))
            {
                throw new InvalidOperationException("Target directory is not specified. Please set it in the configuration or provide it as a parameter.");
            }

            // Create services
            var pathConvertService = new PathConvertService();

            // Aggregate results
            var aggregateResult = new PathConvertResult();
            var fileResults = new Dictionary<string, (PathConvertResult Result, string? M3uFilePath)>();

            // Process each file
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    aggregateResult.NotFoundFiles.Add(filePath);
                    continue;
                }

                try
                {
                    // Read and extract paths from input file
                    var content = PlaylistFileHelper.ReadPlaylistFile(filePath);
                    if (string.IsNullOrEmpty(content))
                    {
                        continue; // Skip empty files
                    }

                    var inputPaths = PlaylistFileHelper.ExtractFilePaths(content, filePath);

                    if (inputPaths.Count == 0)
                    {
                        continue; // Skip empty files
                    }

                    // Process paths - use searchDirectory as removePathPrefix if UseRelativePath is enabled
                    var removePathPrefix = config.UseRelativePath ? searchDirectory : string.Empty;
                    var result = pathConvertService.ProcessFilePaths(
                        inputPaths,
                        searchDirectory,
                        config.AudioExtensions,
                        config.PreferredExtension,
                        removePathPrefix);

                    // Save output M3U file for this input file
                    string? m3uFilePath = null;
                    if (result.OutputFiles.Count > 0)
                    {
                        m3uFilePath = PlaylistFileHelper.SaveOutputM3uFile(filePath, result.OutputFiles);
                    }

                    // Store result for this file (include M3U file path)
                    fileResults[filePath] = (result, m3uFilePath);

                    // Aggregate results
                    aggregateResult.OutputFiles.AddRange(result.OutputFiles);
                    aggregateResult.NotFoundFiles.AddRange(result.NotFoundFiles);
                }
                catch (Exception ex)
                {
                    // Add to not found if processing fails
                    aggregateResult.NotFoundFiles.Add(filePath);
                    System.Diagnostics.Debug.WriteLine($"Failed to process {filePath}: {ex.Message}");
                }
            }

            // Check if there are any not found files - if so, show MainWindow
            if (aggregateResult.NotFoundFiles.Count > 0)
            {
                // Only show window if we're in a WPF application context
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowMainWindow();
                        
                        // Load files and results into the window
                        if (_mainWindow != null && _mainWindow.ViewModel != null)
                        {
                            var vm = _mainWindow.ViewModel;
                            
                            // Add input files
                            foreach (var filePath in filePaths)
                            {
                                if (!vm.InputFiles.Contains(filePath))
                                {
                                    vm.InputFiles.Add(filePath);
                                }
                            }

                            // Set target directory
                            if (!string.IsNullOrWhiteSpace(targetDirectory))
                            {
                                vm.SearchDirectory = targetDirectory;
                            }

                            // Add process results for files with not found items or with M3U files
                            foreach (var kvp in fileResults)
                            {
                                var filePath = kvp.Key;
                                var (result, m3uFilePath) = kvp.Value;

                                // Add result if it has not found files or has M3U file
                                if (result.NotFoundFiles.Count > 0 || !string.IsNullOrEmpty(m3uFilePath))
                                {
                                    var processResult = new ProcessResultViewModel
                                    {
                                        InputFileName = filePath,
                                        MatchedFilesCount = result.OutputFiles.Count,
                                        M3uFilePath = m3uFilePath ?? ""
                                    };
                                    processResult.UpdateNotFoundFiles(result.NotFoundFiles);
                                    vm.ProcessResults.Add(processResult);
                                }
                            }

                            // Select first result (prefer one with not found files, otherwise first with M3U file)
                            var firstResultWithNotFound = vm.ProcessResults.FirstOrDefault(r => r.NotFoundFiles.Count > 0);
                            if (firstResultWithNotFound != null)
                            {
                                vm.SelectedProcessResult = firstResultWithNotFound;
                            }
                            else
                            {
                                var firstResultWithM3u = vm.ProcessResults.FirstOrDefault(r => !string.IsNullOrEmpty(r.M3uFilePath));
                                if (firstResultWithM3u != null)
                                {
                                    vm.SelectedProcessResult = firstResultWithM3u;
                                }
                            }
                        }
                    });
                }
            }

            return aggregateResult;
        }


        /// <summary>
        /// Auto process files with optional target directory selection dialog
        /// </summary>
        /// <param name="filePaths">List of playlist file paths to process (null to open main window)</param>
        /// <param name="targetDirectory">Target directory to search for files (null to show dialog)</param>
        /// <returns>Processing result, or null if cancelled or no files provided</returns>
        public static PathConvertResult? AutoProcess(List<string>? filePaths = null, string? targetDirectory = null)
        {
            // If file paths list is empty or null, just open main window
            if (filePaths == null || filePaths.Count == 0)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowMainWindow();
                    });
                }
                return null;
            }

            // Filter out directories, only keep files
            var validFiles = filePaths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path) && !Directory.Exists(path))
                .ToList();

            if (validFiles.Count == 0)
            {
                MessageHelper.ShowWarning("没有找到有效的文件路径（已过滤掉文件夹）");
                return null;
            }

            // Use filtered file list
            filePaths = validFiles;

            // If target directory is not provided, show dialog
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                string? selectedDirectory = null;

                if (Application.Current != null)
                {
                    // Load config to get default directory
                    var configService = new ConfigService();
                    var config = configService.GetConfig<PathConvertConfig>();
                    var defaultDirectory = config.SearchDirectory;

                    // Show dialog on UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var dialog = new Windows.TargetDirectoryDialog(defaultDirectory);
                        if (dialog.ShowDialog() == true)
                        {
                            selectedDirectory = dialog.TargetDirectory;
                            
                            // Update config with selected directory
                            if (!string.IsNullOrEmpty(selectedDirectory))
                            {
                                config.SearchDirectory = selectedDirectory;
                            }
                        }
                    });
                }

                // User cancelled the dialog
                if (string.IsNullOrWhiteSpace(selectedDirectory))
                {
                    return null;
                }

                targetDirectory = selectedDirectory;
            }

            // Process files with the target directory
            var result = ProcessFiles(filePaths, targetDirectory);
            
            // Show summary message
            var totalFiles = filePaths.Count;
            var outputCount = result.OutputFiles.Count;
            var notFoundCount = result.NotFoundFiles.Count;
            
            if (notFoundCount == 0 && outputCount > 0)
            {
                // All successful
                MessageHelper.ShowSuccess($"处理完成: 成功处理 {totalFiles} 个文件，匹配 {outputCount} 个文件路径");
            }
            else if (outputCount > 0 && notFoundCount > 0)
            {
                // Partial success
                MessageHelper.ShowWarning($"处理完成: 成功处理 {totalFiles} 个文件，匹配 {outputCount} 个文件路径，未找到 {notFoundCount} 个文件");
            }
            else if (notFoundCount > 0)
            {
                // All failed
                MessageHelper.ShowError($"处理失败: 处理了 {totalFiles} 个文件，但未找到 {notFoundCount} 个文件");
            }
            else
            {
                // No results
                MessageHelper.ShowWarning($"处理完成: 处理了 {totalFiles} 个文件，但没有匹配到任何文件");
            }
            
            return result;
        }
    }
}

