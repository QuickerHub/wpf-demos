using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using XmlExtractTool;
using XmlExtractTool.Models;
using XmlExtractTool.Services;

namespace XmlExtractTool.ViewModels
{
    /// <summary>
    /// ViewModel for XML node checker (folder / file, settings, full rules)
    /// </summary>
    public partial class XmlExtractViewModel : ObservableObject
    {
        private readonly XmlQuaternionChecker _checker;
        private readonly XmlNodeChecker _nodeChecker;

        public XmlExtractViewModel()
        {
            Settings = new CheckerSettings();
            Settings.Load();
            _checker = new XmlQuaternionChecker();
            _nodeChecker = new XmlNodeChecker(Settings);
            InvalidNodeNames = new ObservableCollection<string>();
            InvalidResults = new ObservableCollection<CheckResultItem>();
        }

        /// <summary>
        /// Checker settings (file extensions, LoopMode keywords)
        /// </summary>
        [ObservableProperty]
        private CheckerSettings _settings = new();

        /// <summary>
        /// Selected file path (single file mode)
        /// </summary>
        [ObservableProperty]
        private string _filePath = string.Empty;

        /// <summary>
        /// Selected folder path (folder scan mode)
        /// </summary>
        [ObservableProperty]
        private string _folderPath = string.Empty;

        /// <summary>
        /// XML text content (for AvalonEdit display)
        /// </summary>
        [ObservableProperty]
        private string _xmlText = string.Empty;

        /// <summary>
        /// Legacy: list of invalid node names (single-file simple check)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _invalidNodeNames = new();

        /// <summary>
        /// Full results: FileName, NodeName, Parent (folder or single-file full check)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CheckResultItem> _invalidResults = new();

        /// <summary>
        /// Status message
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "请选择文件夹或文件，或输入 XML 文本";

        /// <summary>
        /// Whether checking is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isChecking = false;

        /// <summary>
        /// Total number of nodes checked
        /// </summary>
        [ObservableProperty]
        private int _totalNodes = 0;

        /// <summary>
        /// Number of invalid items
        /// </summary>
        [ObservableProperty]
        private int _invalidNodesCount = 0;

        /// <summary>
        /// Select folder command (folder scan)
        /// </summary>
        [RelayCommand]
        private void SelectFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择要检测的文件夹"
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = dialog.SelectedPath;
                _ = CheckFolderAsync();
            }
        }

        /// <summary>
        /// Open settings window
        /// </summary>
        [RelayCommand]
        private void OpenSettings()
        {
            var win = new SettingsWindow
            {
                DataContext = Settings,
                Owner = Application.Current.MainWindow
            };
            if (win.ShowDialog() == true)
                Settings.Save();
        }

        /// <summary>
        /// Select file command (single file)
        /// </summary>
        [RelayCommand]
        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "支持格式 (*.mil;*.upe;*.xml;*.uff)|*.mil;*.upe;*.xml;*.uff|All files (*.*)|*.*",
                Title = "选择文件"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadInput(dialog.FileName);
            }
        }

        /// <summary>
        /// Set folder path and run folder check (e.g. from Quicker Runner).
        /// </summary>
        public void LoadFolderAndCheck(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;
            FolderPath = folderPath;
            _ = CheckFolderAsync();
        }

        /// <summary>
        /// Load input (file path or XML text) and run full check
        /// </summary>
        public void LoadInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            if (File.Exists(input))
            {
                FilePath = input;
                try
                {
                    XmlText = File.ReadAllText(input);
                    StatusMessage = $"已加载文件: {Path.GetFileName(FilePath)}";
                    if (CanCheck())
                        _ = CheckFileAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"加载文件失败: {ex.Message}";
                    MessageBox.Show($"加载文件失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                FilePath = string.Empty;
                XmlText = input;
                StatusMessage = "已加载 XML 文本内容";
                if (CanCheckFromText())
                    _ = CheckQuaternionsAsync();
            }
        }

        /// <summary>
        /// Check single file (full rules)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheck))]
        private async Task CheckFileAsync()
        {
            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
                return;
            IsChecking = true;
            StatusMessage = "正在检测...";
            InvalidResults.Clear();
            InvalidNodeNames.Clear();
            InvalidNodesCount = 0;
            TotalNodes = 0;
            try
            {
                var list = await Task.Run(() => _nodeChecker.CheckFile(FilePath));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in list)
                        InvalidResults.Add(item);
                    InvalidNodesCount = InvalidResults.Count;
                    var nodeCount = _checker.ParseNodes(FilePath).Count;
                    TotalNodes = nodeCount;
                    if (InvalidNodesCount == 0)
                        StatusMessage = $"检测完成：共 {TotalNodes} 个节点，全部符合条件";
                    else
                        StatusMessage = $"检测完成：共 {TotalNodes} 个节点，{InvalidNodesCount} 项不符合条件";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"检测失败: {ex.Message}";
                MessageBox.Show($"检测过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsChecking = false;
            }
        }

        /// <summary>
        /// Check folder (full rules)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheckFolder))]
        private async Task CheckFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
                return;
            IsChecking = true;
            StatusMessage = "正在检测文件夹...";
            InvalidResults.Clear();
            InvalidNodeNames.Clear();
            InvalidNodesCount = 0;
            TotalNodes = 0;
            try
            {
                var list = await Task.Run(() => _nodeChecker.CheckFolder(FolderPath));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in list)
                        InvalidResults.Add(item);
                    InvalidNodesCount = list.Count;
                    TotalNodes = 0;
                    if (InvalidNodesCount == 0)
                        StatusMessage = "检测完成：无不符合项";
                    else
                        StatusMessage = $"检测完成：{InvalidNodesCount} 项不符合条件";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"检测失败: {ex.Message}";
                MessageBox.Show($"检测过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsChecking = false;
            }
        }

        /// <summary>
        /// Check quaternions from XML text (legacy)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheck))]
        private async Task CheckQuaternionsAsync()
        {
            if (string.IsNullOrWhiteSpace(XmlText))
                return;
            IsChecking = true;
            StatusMessage = "正在检测...";
            InvalidNodeNames.Clear();
            InvalidResults.Clear();
            InvalidNodesCount = 0;
            TotalNodes = 0;
            try
            {
                var invalidNames = await Task.Run(() => _checker.CheckQuaternionsFromText(XmlText));
                var allNodes = await Task.Run(() => _checker.ParseNodesFromText(XmlText));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var name in invalidNames)
                    {
                        InvalidNodeNames.Add(name);
                        InvalidResults.Add(new CheckResultItem { FileName = "", NodeName = name, Parent = "" });
                    }
                    TotalNodes = allNodes.Count;
                    InvalidNodesCount = invalidNames.Count;
                    if (InvalidNodesCount == 0)
                        StatusMessage = $"检测完成：共 {TotalNodes} 个节点，全部符合 90 度旋转条件";
                    else
                        StatusMessage = $"检测完成：共 {TotalNodes} 个节点，{InvalidNodesCount} 个不符合 90 度旋转条件";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"检测失败: {ex.Message}";
                MessageBox.Show($"检测过程中发生错误:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsChecking = false;
            }
        }

        private bool CanCheckFolder() => !IsChecking && !string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath);

        /// <summary>
        /// Check if can execute check command
        /// </summary>
        private bool CanCheck()
        {
            return !IsChecking && (CanCheckFromFile() || CanCheckFromText());
        }

        /// <summary>
        /// Check if can check from file
        /// </summary>
        private bool CanCheckFromFile()
        {
            return !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
        }

        /// <summary>
        /// Check if can check from text
        /// </summary>
        private bool CanCheckFromText()
        {
            return !string.IsNullOrWhiteSpace(XmlText);
        }

        /// <summary>
        /// Copy results to clipboard (format: 文件名 / Node Name / Parent)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCopyResults))]
        private void CopyResults()
        {
            if (InvalidResults.Count > 0)
            {
                var text = string.Join(Environment.NewLine + Environment.NewLine, InvalidResults.Select(r => r.ToString()));
                Clipboard.SetText(text);
                StatusMessage = $"已复制 {InvalidResults.Count} 项到剪贴板";
                return;
            }
            if (InvalidNodeNames.Count > 0)
            {
                var text = string.Join(Environment.NewLine, InvalidNodeNames);
                Clipboard.SetText(text);
                StatusMessage = $"已复制 {InvalidNodeNames.Count} 个节点名称到剪贴板";
            }
        }

        private bool CanCopyResults()
        {
            return InvalidResults.Count > 0 || InvalidNodeNames.Count > 0;
        }

        /// <summary>
        /// Save results to file (format: 文件名 / Node Name / Parent)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveResults))]
        private void SaveResults()
        {
            if (InvalidResults.Count == 0 && InvalidNodeNames.Count == 0)
                return;

            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "保存结果",
                FileName = "invalid_nodes.txt"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                if (InvalidResults.Count > 0)
                    File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine + Environment.NewLine, InvalidResults.Select(r => r.ToString())));
                else
                    File.WriteAllLines(dialog.FileName, InvalidNodeNames);
                StatusMessage = $"结果已保存到: {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show($"结果已保存到:\n{dialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveResults()
        {
            return InvalidResults.Count > 0 || InvalidNodeNames.Count > 0;
        }

        // Update command states when properties change
        partial void OnFilePathChanged(string value)
        {
            CheckQuaternionsCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsCheckingChanged(bool value)
        {
            CheckQuaternionsCommand.NotifyCanExecuteChanged();
        }

        partial void OnInvalidNodeNamesChanged(ObservableCollection<string> value)
        {
            CopyResultsCommand.NotifyCanExecuteChanged();
            SaveResultsCommand.NotifyCanExecuteChanged();
            if (value != null)
                value.CollectionChanged += (s, e) => { CopyResultsCommand.NotifyCanExecuteChanged(); SaveResultsCommand.NotifyCanExecuteChanged(); };
        }

        partial void OnInvalidResultsChanged(ObservableCollection<CheckResultItem> value)
        {
            CopyResultsCommand.NotifyCanExecuteChanged();
            SaveResultsCommand.NotifyCanExecuteChanged();
            if (value != null)
                value.CollectionChanged += (s, e) => { CopyResultsCommand.NotifyCanExecuteChanged(); SaveResultsCommand.NotifyCanExecuteChanged(); };
        }

        partial void OnFolderPathChanged(string value)
        {
            CheckFolderCommand.NotifyCanExecuteChanged();
        }
    }
}
