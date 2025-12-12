using System.Collections.Generic;
using System.IO;
using System.Windows;
using BatchRenameTool.Template.Parser;
using BatchRenameTool.Template.Evaluator;
using BatchRenameTool.ViewModels;
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

#if DEBUG
            // Auto-load test folder in debug mode
            const string debugFolder = @"C:\Users\ldy\Desktop\cmm";
            if (Directory.Exists(debugFolder))
            {
                _viewModel.AddFilesCommand.Execute(debugFolder);
            }

            // Test template parsing
            TestTemplateParsing();
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

        private void TestTemplateParsing()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting Template Parsing Test ===");

                var parser = new TemplateParser(new List<System.Type>());
                var evaluator = new TemplateEvaluator();

                System.Diagnostics.Debug.WriteLine("Parser and evaluator created");

                var testCases = new[]
                {
                    "{name.upper()}",
                    "{name.replace('e','E')}",
                    "{name.sub(1,3)}"
                };

                foreach (var template in testCases)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Testing template: {template}");

                        var context = new EvaluationContext
                        {
                            Name = "test",
                            Ext = "txt",
                            FullName = "test.txt",
                            Index = 0
                        };

                        System.Diagnostics.Debug.WriteLine("Parsing template...");
                        var node = parser.Parse(template);
                        System.Diagnostics.Debug.WriteLine("Template parsed successfully");

                        System.Diagnostics.Debug.WriteLine("Evaluating template...");
                        var result = evaluator.Evaluate(node, context);
                        System.Diagnostics.Debug.WriteLine($"Template: {template} -> Result: {result}");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error testing {template}: {ex2.Message}");
                        System.Diagnostics.Debug.WriteLine(ex2.StackTrace);
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== Template Parsing Test Completed ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Template test error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
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
        /// Handle help button click - toggle popup
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = !HelpPopup.IsOpen;
        }

        /// <summary>
        /// Handle close help popup button click
        /// </summary>
        private void CloseHelpPopup_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = false;
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
    }
}
