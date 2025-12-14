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
                var availableWidth = FileListView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 20; // Reserve some space for padding
                if (availableWidth > 0)
                {
                    // Divide space equally between the two columns
                    var columnWidth = availableWidth / 2.0;
                    gridView.Columns[0].Width = columnWidth; // 重命名前
                    gridView.Columns[1].Width = columnWidth; // 重命名后
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
        /// Organized in hierarchical menu structure with high information density
        /// </summary>
        private void BuildDemoMenu()
        {
            if (DemoMenu == null)
                return;

            // 字符串截取 [a:b] 重点菜单（多级菜单）
            var sliceMenu = new System.Windows.Controls.MenuItem
            {
                Header = "字符串截取 [a:b] ⭐",
                ToolTip = "重点语法：使用 [a:b] 截取文件名"
            };

            var sliceExamples = new List<(string Pattern, string Description, string Example)>
            {
                // 基础截取语法
                ("{name[:5]}_{i:00}.{ext}", "前5字符+序号", "docum_00.txt ← document.txt"),
                ("{name[4:]}.{ext}", "跳过前4字符", "photo.jpg ← IMG_photo.jpg"),
                ("{name[1:4]}.{ext}", "索引1-4", "ile ← file.txt"),
                ("{name[:3]}_{name[3:]}.{ext}", "前3+剩余", "ABC_123file.txt ← ABC123file.txt"),
                
                // 去除前缀/后缀
                ("{name[7:]}.{ext}", "去除前缀", "filename.txt ← prefix_filename.txt"),
                ("{name[:7]}_new.{ext}", "去除后缀", "oldfile_new.txt ← oldfile_backup.txt"),
                
                // 提取中间部分
                ("{name[2:6]}_{i:00}.{ext}", "提取中间+序号", "2401_00.pdf ← 20240101_report.pdf"),
                ("{name[4:6]}-{name[6:8]}-{name[8:10]}_{name[11:]}.{ext}", "日期转换", "01-15-report.txt ← 20240115_report.txt"),
                
                // 截取+序号
                ("{name[:8]}_{i:000}.{ext}", "前8字符+3位序号", "longfile_000.txt"),
                
                // 截取+方法组合（重点）
                ("{name[:10].replace(_,-)}.{ext}", "截取+替换", "file-name-.txt ← file_name_with_underscores.txt"),
                ("{name[:4].upper}_{name[4:]}.{ext}", "前4大写+剩余", "TEST_file.txt ← testfile.txt"),
                ("{name[4:].upper.replace(_,-)}.{ext}", "跳过+大写+替换", "PHOTO-JPG ← IMG_photo.jpg"),
            };

            AddChildMenus(sliceMenu, sliceExamples);
            DemoMenu.Items.Add(sliceMenu);

            // 分隔线
            DemoMenu.Items.Add(new System.Windows.Controls.Separator());

            // 基础变量菜单
            var basicMenu = new System.Windows.Controls.MenuItem
            {
                Header = "基础变量"
            };

            var basicExamples = new List<(string Pattern, string Description)>
            {
                ("{i:001}_{name}.{ext}", "3位序号(001)"),
                ("{i:01}_{name}.{ext}", "2位序号(01)"),
                ("{i:1}_{name}.{ext}", "从1开始"),
                ("{iv:001}_{name}.{ext}", "倒序序号"),
                ("{i:2*i+1:000}_{name}.{ext}", "表达式(2*i+1)"),
                ("{i:i*3-2:00}_{name}.{ext}", "表达式(i*3-2)"),
            };

            AddChildMenus(basicMenu, basicExamples);
            DemoMenu.Items.Add(basicMenu);

            // 字符串方法菜单
            var methodMenu = new System.Windows.Controls.MenuItem
            {
                Header = "字符串方法"
            };

            var methodExamples = new List<(string Pattern, string Description)>
            {
                ("{name.upper}.{ext}", "转大写"),
                ("{name.lower}.{ext}", "转小写"),
                ("{name.trim}.{ext}", "去首尾空格"),
                ("{name.replace(_,-)}.{ext}", "下划线→短横线"),
                ("{name.replace( ,)}.{ext}", "删除空格"),
                ("{i:001}_{name.upper}.{ext}", "序号+大写"),
                ("{name.upper.replace(_,-)}.{ext}", "大写+替换"),
                ("{name.replace(old,new)}.{ext}", "自定义替换"),
            };

            AddChildMenus(methodMenu, methodExamples);
            DemoMenu.Items.Add(methodMenu);

            // 日期时间菜单
            var dateMenu = new System.Windows.Controls.MenuItem
            {
                Header = "日期时间"
            };

            var dateExamples = new List<(string Pattern, string Description)>
            {
                ("{today:yyyyMMdd}_{name}.{ext}", "日期(yyyyMMdd)"),
                ("{today:yyyy-MM-dd}_{name}.{ext}", "日期(yyyy-MM-dd)"),
                ("{today:yyyy年MM月dd日}_{name}.{ext}", "日期(中文)"),
                ("{now:yyyyMMddHHmmss}_{name}.{ext}", "日期时间(精确到秒)"),
                ("{now:yyyyMMddHHmm}_{name}.{ext}", "日期时间(精确到分)"),
                ("{i:001}_{today:yyyyMMdd}_{name}.{ext}", "序号+日期"),
            };

            AddChildMenus(dateMenu, dateExamples);
            DemoMenu.Items.Add(dateMenu);

            // 组合使用菜单
            var comboMenu = new System.Windows.Controls.MenuItem
            {
                Header = "组合使用"
            };

            var comboExamples = new List<(string Pattern, string Description)>
            {
                ("{today:yyyyMMdd}_{name.upper}_{i:000}.{ext}", "日期+大写+序号"),
                ("{i:2*i+1:000}_{name.lower}.{ext}", "表达式+小写"),
                ("{name[:5].replace(_,-).upper}_{i:00}.{ext}", "截取+替换+大写+序号"),
                ("{name[4:].upper.replace(_,-)}.{ext}", "跳过+大写+替换"),
                ("{i:001}_{today:yyyyMMdd}_{name[:8]}.{ext}", "序号+日期+截取"),
            };

            AddChildMenus(comboMenu, comboExamples);
            DemoMenu.Items.Add(comboMenu);

            // 分隔线
            DemoMenu.Items.Add(new System.Windows.Controls.Separator());

            // 其他常用
            var otherExamples = new List<(string Pattern, string Description)>
            {
                ("{fullname}", "保持原文件名不变"),
                ("{name}_backup.{ext}", "添加 backup 后缀"),
                ("New_{fullname}", "添加 New_ 前缀"),
            };

            AddChildMenus(DemoMenu, otherExamples);
        }

        /// <summary>
        /// Add child menu items to a parent menu item with pattern examples (with example)
        /// </summary>
        private void AddChildMenus(System.Windows.Controls.MenuItem parentMenu, IEnumerable<(string Pattern, string Description, string Example)> examples)
        {
            foreach (var (pattern, description, example) in examples)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = $"{description} | {example}",
                    ToolTip = $"模式: {pattern}\n示例: {example}"
                };
                menuItem.Click += (s, e) =>
                {
                    _viewModel.RenamePattern = pattern;
                };
                parentMenu.Items.Add(menuItem);
            }
        }

        /// <summary>
        /// Add child menu items to a parent menu item with pattern examples (without example)
        /// </summary>
        private void AddChildMenus(System.Windows.Controls.MenuItem parentMenu, IEnumerable<(string Pattern, string Description)> examples)
        {
            foreach (var (pattern, description) in examples)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = $"{description} | {pattern}",
                    ToolTip = pattern
                };
                menuItem.Click += (s, e) =>
                {
                    _viewModel.RenamePattern = pattern;
                };
                parentMenu.Items.Add(menuItem);
            }
        }

        /// <summary>
        /// Add child menu items directly to the demo menu (for top-level items)
        /// </summary>
        private void AddChildMenus(System.Windows.Controls.ItemsControl parentMenu, IEnumerable<(string Pattern, string Description)> examples)
        {
            foreach (var (pattern, description) in examples)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = $"{description} | {pattern}",
                    ToolTip = pattern
                };
                menuItem.Click += (s, e) =>
                {
                    _viewModel.RenamePattern = pattern;
                };
                parentMenu.Items.Add(menuItem);
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
