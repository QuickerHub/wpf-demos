using System.IO;
using System.Windows;
using BatchRenameTool.ViewModels;

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
            _viewModel = new BatchRenameViewModel();
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
    }
}
