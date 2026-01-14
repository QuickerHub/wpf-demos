using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using BatchRenameTool.Controls;
using BatchRenameTool.Services;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.ViewModels;
using BatchRenameTool.Windows;
using System;
using System.Linq;

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

            // Update column widths when DataGrid size changes
            FileDataGrid.SizeChanged += (s, args) => UpdateColumnWidths();

            // Update remove button state when selection changes
            FileDataGrid.SelectionChanged += (s, args) =>
            {
                RemoveSelectedButton.IsEnabled = FileDataGrid.SelectedItems.Count > 0;
            };

            // Setup keyboard shortcuts for paste and delete
            FileDataGrid.KeyDown += FileDataGrid_KeyDown;
            FileDataGrid.PreviewKeyDown += FileDataGrid_PreviewKeyDown;

            // Setup lazy evaluation: calculate NewName only when items become visible
            SetupLazyEvaluation();

            // Setup lazy evaluation: calculate NewName only when items become visible
            SetupLazyEvaluation();

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
            if (FileDataGrid?.Columns != null && FileDataGrid.Columns.Count >= 3)
            {
                var availableWidth = FileDataGrid.ActualWidth - SystemParameters.VerticalScrollBarWidth - 20; // Reserve some space for padding
                if (availableWidth > 0)
                {
                    // Divide space equally between the three columns
                    var columnWidth = availableWidth / 3.0;
                    FileDataGrid.Columns[0].Width = columnWidth; // 重命名前
                    FileDataGrid.Columns[1].Width = columnWidth; // 自定义名称
                    FileDataGrid.Columns[2].Width = columnWidth; // 重命名后
                }
            }
        }

        /// <summary>
        /// Setup lazy evaluation: calculate NewName only when items become visible
        /// </summary>
        private void SetupLazyEvaluation()
        {
            // Listen to scroll events to calculate newly visible items
            FileDataGrid.Loaded += (s, e) =>
            {
                // Find ScrollViewer in DataGrid template
                var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(FileDataGrid);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged += (sender, args) =>
                    {
                        CalculateVisibleItems();
                    };
                }
            };

            // Also listen to layout updates
            FileDataGrid.LayoutUpdated += (s, e) =>
            {
                CalculateVisibleItems();
            };
        }

        /// <summary>
        /// Find a visual child of a specific type
        /// </summary>
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Calculate NewName for all currently visible items
        /// </summary>
        private void CalculateVisibleItems()
        {
            if (_viewModel == null || FileDataGrid.ItemsSource == null)
                return;

            var items = _viewModel.Items;
            if (items == null || items.Count == 0)
                return;

            // Get visible items using DataGrid's row containers
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < items.Count; i++)
            {
                var row = FileDataGrid.ItemContainerGenerator.ContainerFromIndex(i) as System.Windows.Controls.DataGridRow;
                if (row != null && row.IsVisible)
                {
                    // Check if row is actually visible in viewport
                    var isVisible = row.IsVisible && 
                                   row.ActualHeight > 0 && 
                                   row.ActualWidth > 0;
                    
                    if (isVisible && items[i] is ViewModels.FileRenameItem item && item.NeedsRecalculation)
                    {
                        processedIndices.Add(i);
                        // Calculate NewName for this visible item
                        _viewModel.CalculateItemNewName(item, i);
                    }
                }
            }
        }

        /// <summary>
        /// Handle folder selection button click
        /// </summary>
        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = FileDialog.ShowFolderBrowserDialog(
                description: "选择包含要重命名文件的文件夹",
                selectedPath: null,
                showNewFolderButton: true);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.ResetFilesFromFolderCommand.Execute(selectedPath);
            }
        }



        /// <summary>
        /// Handle remove selected items button click
        /// </summary>
        private void RemoveSelectedItemsButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileDataGrid.SelectedItems.Count > 0)
            {
                _viewModel.RemoveItemsCommand.Execute(FileDataGrid.SelectedItems);
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
                ("{dirname}_{name}.{ext}", "文件夹名+文件名"),
                ("{dirname}_{i:001}_{name}.{ext}", "文件夹名+序号+文件名"),
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

        /// <summary>
        /// Handle key down events for paste and delete operations
        /// </summary>
        private void FileDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+V for paste
            if (e.Key == Key.V && 
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                HandlePasteOperation();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Preview key down to handle keyboard shortcuts before DataGrid processes them
        /// </summary>
        private void FileDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+V for paste
            if (e.Key == Key.V && 
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Focus DataGrid if not already focused
                if (!FileDataGrid.IsFocused)
                {
                    FileDataGrid.Focus();
                }
                HandlePasteOperation();
                e.Handled = true;
            }
            // Handle Delete key for clearing custom names
            // Process in PreviewKeyDown to intercept before DataGrid handles it
            else if (e.Key == Key.Delete)
            {
                // Check if focus is on a TextBox (editing mode)
                var focusedElement = Keyboard.FocusedElement;
                if (focusedElement is System.Windows.Controls.TextBox)
                {
                    // Let the textbox handle delete normally when editing
                    return;
                }
                
                HandleDeleteOperation();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle paste operation: paste multi-line text to selected items (Excel-style)
        /// </summary>
        private void HandlePasteOperation()
        {
            try
            {
                // Check if clipboard contains text
                if (!System.Windows.Clipboard.ContainsText())
                    return;

                // Get clipboard text
                var clipboardText = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                    return;

                // Get selected items
                var selectedItems = FileDataGrid.SelectedItems;
                if (selectedItems == null || selectedItems.Count == 0)
                    return;

                // Split text by line breaks (support \r\n, \n, \r)
                var lines = clipboardText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                // Filter out empty lines
                var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                
                // Convert selected items to list of FileRenameItem
                var itemsList = selectedItems.Cast<ViewModels.FileRenameItem>().ToList();
                
                // If only one item is selected but multiple lines in clipboard, ask user
                if (itemsList.Count == 1 && nonEmptyLines.Length > 1)
                {
                    var result = MessageBox.Show(
                        $"剪贴板中有 {nonEmptyLines.Length} 行文本，但只选中了 1 行。\n\n" +
                        "选择操作：\n" +
                        "• 是：只粘贴第一行到当前选中项\n" +
                        "• 否：自动扩展选中项，粘贴所有行（从当前项开始）",
                        "粘贴确认",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Only paste first line
                        itemsList[0].CustomName = nonEmptyLines[0].Trim();
                        return;
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // Expand selection to include more items for multi-line paste
                        var currentIndex = FileDataGrid.Items.IndexOf(itemsList[0]);
                        if (currentIndex >= 0)
                        {
                            // Clear current selection
                            FileDataGrid.SelectedItems.Clear();
                            
                            // Select items starting from current index
                            int endIndex = Math.Min(currentIndex + nonEmptyLines.Length - 1, FileDataGrid.Items.Count - 1);
                            for (int i = currentIndex; i <= endIndex; i++)
                            {
                                var item = FileDataGrid.Items[i];
                                FileDataGrid.SelectedItems.Add(item);
                            }
                            
                            // Update itemsList with new selection
                            itemsList = FileDataGrid.SelectedItems.Cast<ViewModels.FileRenameItem>().ToList();
                        }
                        else
                        {
                            // If can't find index, just paste first line
                            itemsList[0].CustomName = nonEmptyLines[0].Trim();
                            return;
                        }
                    }
                    else
                    {
                        // User cancelled
                        return;
                    }
                }
                
                // Paste lines to selected items (Excel-style: first line to first item, etc.)
                int minCount = Math.Min(nonEmptyLines.Length, itemsList.Count);
                for (int i = 0; i < minCount; i++)
                {
                    // Trim the line and assign to CustomName
                    itemsList[i].CustomName = nonEmptyLines[i].Trim();
                }
            }
            catch (Exception ex)
            {
                // Silently fail or show error message
                System.Diagnostics.Debug.WriteLine($"Paste operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle delete operation: clear custom names of selected items
        /// </summary>
        private void HandleDeleteOperation()
        {
            try
            {
                // Get selected items
                var selectedItems = FileDataGrid.SelectedItems;
                if (selectedItems == null || selectedItems.Count == 0)
                    return;

                // Clear CustomName for all selected items
                foreach (ViewModels.FileRenameItem item in selectedItems)
                {
                    item.CustomName = "";
                }
            }
            catch (Exception ex)
            {
                // Silently fail or show error message
                System.Diagnostics.Debug.WriteLine($"Delete operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle move to next cell when Enter is pressed in EditableCell
        /// </summary>
        private void EditableCell_MoveToNextCell(object? sender, EventArgs e)
        {
            if (sender is not Controls.EditableCell currentCell)
                return;

            // Find the DataGridRow containing this cell
            var row = FindVisualParent<System.Windows.Controls.DataGridRow>(currentCell);
            if (row == null)
                return;

            // Get the current row index
            var currentIndex = FileDataGrid.Items.IndexOf(row.Item);
            if (currentIndex < 0 || currentIndex >= FileDataGrid.Items.Count - 1)
                return; // Already at the last row

            // Find the next row
            var nextRow = FileDataGrid.ItemContainerGenerator.ContainerFromIndex(currentIndex + 1) as System.Windows.Controls.DataGridRow;
            if (nextRow == null)
            {
                // If next row is not generated yet, scroll to it first
                FileDataGrid.ScrollIntoView(FileDataGrid.Items[currentIndex + 1]);
                // Wait for row to be generated
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    nextRow = FileDataGrid.ItemContainerGenerator.ContainerFromIndex(currentIndex + 1) as System.Windows.Controls.DataGridRow;
                    if (nextRow != null)
                    {
                        MoveToNextCellInRow(nextRow);
                    }
                }));
                return;
            }

            MoveToNextCellInRow(nextRow);
        }

        /// <summary>
        /// Move to EditableCell in the specified row
        /// </summary>
        private void MoveToNextCellInRow(System.Windows.Controls.DataGridRow row)
        {
            // Find the EditableCell in the row (same column - "自定义名称" column is index 1)
            var nextCell = FindVisualChild<Controls.EditableCell>(row);
            if (nextCell != null)
            {
                // Scroll the next row into view
                row.BringIntoView();
                
                // Start editing the next cell
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                {
                    nextCell.IsEditing = true;
                }));
            }
        }

        /// <summary>
        /// Find visual parent of a specific type
        /// </summary>
        private static T? FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
