using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActionPathConvert.Windows
{
    /// <summary>
    /// Dialog for selecting target directory
    /// </summary>
    public partial class TargetDirectoryDialog : Window
    {
        private readonly TargetDirectoryDialogViewModel _viewModel;

        /// <summary>
        /// Selected target directory path
        /// </summary>
        public string? TargetDirectory => _viewModel.TargetDirectory;

        public TargetDirectoryDialog(string? initialDirectory = null)
        {
            InitializeComponent();
            _viewModel = new TargetDirectoryDialogViewModel
            {
                TargetDirectory = initialDirectory ?? ""
            };
            DataContext = _viewModel;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = FileDialog.ShowFolderBrowserDialog(
                description: "选择目标文件夹",
                selectedPath: _viewModel.TargetDirectory,
                showNewFolderButton: true);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.TargetDirectory = selectedPath;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.TargetDirectory))
            {
                MessageBox.Show("请选择目标文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(_viewModel.TargetDirectory))
            {
                MessageBox.Show("目标文件夹不存在，请重新选择", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// ViewModel for TargetDirectoryDialog
    /// </summary>
    public class TargetDirectoryDialogViewModel : ObservableObject
    {
        private string _targetDirectory = "";

        public string TargetDirectory
        {
            get => _targetDirectory;
            set => SetProperty(ref _targetDirectory, value);
        }
    }
}

