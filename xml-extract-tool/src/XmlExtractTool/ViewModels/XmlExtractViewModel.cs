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
using XmlExtractTool.Services;

namespace XmlExtractTool.ViewModels
{
    /// <summary>
    /// ViewModel for XML quaternion checker
    /// </summary>
    public partial class XmlExtractViewModel : ObservableObject
    {
        private readonly XmlQuaternionChecker _checker;

        public XmlExtractViewModel()
        {
            _checker = new XmlQuaternionChecker();
            InvalidNodeNames = new ObservableCollection<string>();
        }

        /// <summary>
        /// Selected XML file path
        /// </summary>
        [ObservableProperty]
        private string _filePath = string.Empty;

        /// <summary>
        /// XML text content (for AvalonEdit display)
        /// </summary>
        [ObservableProperty]
        private string _xmlText = string.Empty;

        /// <summary>
        /// List of Node names that don't satisfy 90-degree rotation condition
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _invalidNodeNames = new();

        /// <summary>
        /// Status message
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "请选择 XML 文件或输入 XML 文本";

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
        /// Number of invalid nodes (not 90-degree rotation)
        /// </summary>
        [ObservableProperty]
        private int _invalidNodesCount = 0;

        /// <summary>
        /// Select XML file command
        /// </summary>
        [RelayCommand]
        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|XML files (*.xml)|*.xml",
                Title = "选择文件"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadInput(dialog.FileName);
            }
        }

        /// <summary>
        /// Load input (file path or XML text) and automatically detect type
        /// </summary>
        public void LoadInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            // Check if input is a file path
            if (File.Exists(input))
            {
                FilePath = input;
                try
                {
                    XmlText = File.ReadAllText(input);
                    StatusMessage = $"已加载文件: {Path.GetFileName(FilePath)}";
                    
                    // Automatically start checking
                    if (CanCheck())
                    {
                        _ = CheckQuaternionsAsync();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"加载文件失败: {ex.Message}";
                    MessageBox.Show($"加载文件失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Treat as XML text content
                FilePath = string.Empty;
                XmlText = input;
                StatusMessage = "已加载 XML 文本内容";
                
                // Automatically start checking
                if (CanCheckFromText())
                {
                    _ = CheckQuaternionsAsync();
                }
            }
        }

        /// <summary>
        /// Check quaternions command
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheck))]
        private async Task CheckQuaternionsAsync()
        {
            IsChecking = true;
            StatusMessage = "正在检测...";
            InvalidNodeNames.Clear();
            InvalidNodesCount = 0;
            TotalNodes = 0;

            try
            {
                await Task.Run(() =>
                {
                    List<string> invalidNames;
                    List<Models.NodeInfo> allNodes;

                    // Check if using file path or text
                    if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                    {
                        invalidNames = _checker.CheckQuaternions(FilePath);
                        allNodes = _checker.ParseNodes(FilePath);
                    }
                    else if (!string.IsNullOrWhiteSpace(XmlText))
                    {
                        invalidNames = _checker.CheckQuaternionsFromText(XmlText);
                        allNodes = _checker.ParseNodesFromText(XmlText);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = "请先选择文件或输入 XML 文本";
                        });
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        InvalidNodeNames.Clear();
                        foreach (var name in invalidNames)
                        {
                            InvalidNodeNames.Add(name);
                        }

                        TotalNodes = allNodes.Count;
                        InvalidNodesCount = invalidNames.Count;

                        if (InvalidNodesCount == 0)
                        {
                            StatusMessage = $"检测完成：共 {TotalNodes} 个节点，全部符合 90 度旋转条件";
                        }
                        else
                        {
                            StatusMessage = $"检测完成：共 {TotalNodes} 个节点，{InvalidNodesCount} 个不符合 90 度旋转条件";
                        }
                    });
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
        /// Copy results to clipboard command
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCopyResults))]
        private void CopyResults()
        {
            if (InvalidNodeNames.Count == 0)
                return;

            var text = string.Join(Environment.NewLine, InvalidNodeNames);
            Clipboard.SetText(text);
            StatusMessage = $"已复制 {InvalidNodeNames.Count} 个节点名称到剪贴板";
        }

        /// <summary>
        /// Check if can copy results
        /// </summary>
        private bool CanCopyResults()
        {
            return InvalidNodeNames.Count > 0;
        }

        /// <summary>
        /// Save results to file command
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveResults))]
        private void SaveResults()
        {
            if (InvalidNodeNames.Count == 0)
                return;

            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "保存结果",
                FileName = "invalid_nodes.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllLines(dialog.FileName, InvalidNodeNames);
                    StatusMessage = $"结果已保存到: {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show($"结果已保存到:\n{dialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败:\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Check if can save results
        /// </summary>
        private bool CanSaveResults()
        {
            return InvalidNodeNames.Count > 0;
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
            {
                value.CollectionChanged += (s, e) =>
                {
                    CopyResultsCommand.NotifyCanExecuteChanged();
                    SaveResultsCommand.NotifyCanExecuteChanged();
                };
            }
        }
    }
}
