using System.Collections.Generic;
using System.IO;
using System.Windows;
using BatchRenameTool.Controls;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.ViewModels;
using BatchRenameTool.Windows;
using System;

namespace BatchRenameTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BatchRenameViewModel _viewModel;

        public BatchRenameViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            // Create TemplateParser and ViewModel directly
            var parser = new TemplateParser(new List<System.Type>());
            _viewModel = new BatchRenameViewModel(parser);
            DataContext = this;

            // Set equal column widths for GridView
            Loaded += MainWindow_Loaded;
            Activated += (s, e) =>
            {
                RenamePatternTextBox?.FocusEditor();
            };
            // Build demo menu
            BuildDemoMenu();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set equal column widths after window is loaded
            UpdateColumnWidths();

            // Update column widths when ListView size changes
            FileListView.SizeChanged += (s, args) => UpdateColumnWidths();

            // Update remove button state when selection changes
            FileListView.SelectionChanged += (s, args) =>
            {
                RemoveSelectedButton.IsEnabled = FileListView.SelectedItems.Count > 0;
            };

            // Setup history popup
            if (HistoryPopupButton?.PopupContent is PatternHistoryListControl historyListControl)
            {
                historyListControl.SetPatterns(_viewModel.PatternHistoryConfig);
                historyListControl.PatternSelected += HistoryListControl_PatternSelected;
            }

            // Auto-focus rename pattern input box when window is loaded
            RenamePatternTextBox?.FocusEditor();

#if DEBUG
            // Auto-load test folder in debug mode
            const string debugFolder = @"C:\Users\ldy\Desktop\cmm";
            if (Directory.Exists(debugFolder))
            {
                _viewModel.AddFilesCommand.Execute(debugFolder);
            }

#endif
        }

        private void UpdateColumnWidths()
        {
            if (FileListView?.View is System.Windows.Controls.GridView gridView && gridView.Columns.Count >= 2)
            {
                var availableWidth = FileListView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
                if (availableWidth > 0)
                {
                    var columnWidth = availableWidth / 2.0;
                    gridView.Columns[0].Width = columnWidth;
                    gridView.Columns[1].Width = columnWidth;
                }
            }
        }

        /// <summary>
        /// Handle folder selection button click
        /// </summary>
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择包含要重命名文件的文件夹"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.AddFilesCommand.Execute(dialog.SelectedPath);
            }
        }



        /// <summary>
        /// Handle remove selected items button click
        /// </summary>
        private void RemoveSelectedItemsButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItems.Count > 0)
            {
                _viewModel.RemoveItemsCommand.Execute(FileListView.SelectedItems);
            }
        }

        /// <summary>
        /// Handle add prefix button click
        /// </summary>
        private void AddPrefixButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog
            {
                Title = "输入前缀",
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var currentPattern = _viewModel.RenamePattern;
                var newPattern = TemplatePatternManager.GeneratePrefixPattern(dialog.InputText, currentPattern);
                _viewModel.RenamePattern = newPattern;
            }
        }

        /// <summary>
        /// Handle add suffix button click
        /// </summary>
        private void AddSuffixButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog
            {
                Title = "输入后缀",
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var currentPattern = _viewModel.RenamePattern;
                var newPattern = TemplatePatternManager.GenerateSuffixPattern(dialog.InputText, currentPattern);
                _viewModel.RenamePattern = newPattern;
            }
        }

        /// <summary>
        /// Build demo menu with various pattern examples
        /// </summary>
        private void BuildDemoMenu()
        {
            if (DemoMenu == null)
                return;

            var demoPatterns = new List<(string Pattern, string Description)>
            {
                // Basic patterns
                ("{i:001}_{name}.{ext}", "序号(001) + 原文件名"),
                ("{i:01}_{name}.{ext}", "序号(01) + 原文件名"),
                ("{i:1}_{name}.{ext}", "序号(1) + 原文件名"),
                ("{iv:001}_{name}.{ext}", "倒序序号(001) + 原文件名"),
                
                // Expression patterns
                ("{i2+1:000}_{fullname}", "表达式序号(2*i+1) + 完整文件名"),
                ("{i3-2:00}_{name}.{ext}", "表达式序号(i*3-2) + 原文件名"),
                ("{i+10:0000}_{name}.{ext}", "表达式序号(i+10) + 原文件名"),
                
                // String method patterns
                ("{name.upper}.{ext}", "大写文件名"),
                ("{name.lower}.{ext}", "小写文件名"),
                ("{name.trim}.{ext}", "去除首尾空格的文件名"),
                ("{i:001}_{name.upper}.{ext}", "序号 + 大写文件名"),
                
                // Date patterns
                ("{today:yyyyMMdd}_{name}.{ext}", "日期(yyyyMMdd) + 文件名"),
                ("{today:yyyy-MM-dd}_{name}.{ext}", "日期(yyyy-MM-dd) + 文件名"),
                ("{today:yyyy年MM月dd日}_{name}.{ext}", "日期(中文) + 文件名"),
                ("{i:001}_{today:yyyyMMdd}_{name}.{ext}", "序号 + 日期 + 文件名"),
                
                // DateTime patterns
                ("{now:yyyyMMddHHmmss}_{name}.{ext}", "日期时间(yyyyMMddHHmmss) + 文件名"),
                ("{now:yyyyMMddHHmm}_{name}.{ext}", "日期时间(yyyyMMddHHmm) + 文件名"),
                ("{now:yyyy年MM月dd日 HH时mm分}_{name}.{ext}", "日期时间(中文) + 文件名"),
                
                // Complex patterns
                ("{i:001}_{name.replace(old,new)}.{ext}", "序号 + 替换后的文件名"),
                ("{today:yyyyMMdd}_{name.upper}_{i:000}.{ext}", "日期 + 大写文件名 + 序号"),
                ("{i2+1:000}_{name.lower}.{ext}", "表达式序号 + 小写文件名"),
                ("{fullname}", "保持原文件名不变"),
            };

            foreach (var (pattern, description) in demoPatterns)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = description,
                    ToolTip = pattern
                };
                menuItem.Click += (s, e) =>
                {
                    _viewModel.RenamePattern = pattern;
                };
                DemoMenu.Items.Add(menuItem);
            }
        }

        /// <summary>
        /// Handle demo button click - show context menu
        /// </summary>
        private void DemoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        /// <summary>
        /// Handle pattern selection from history list
        /// </summary>
        private void HistoryListControl_PatternSelected(object? sender, Models.PatternHistoryItem item)
        {
            if (item != null)
            {
                _viewModel.RenamePattern = item.Pattern;
                // Close the popup
                HistoryPopupButton?.ClosePopup();
            }
        }
    }
}
