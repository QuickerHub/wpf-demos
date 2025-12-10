using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ActionPathConvert.ViewModels;

namespace ActionPathConvert
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = this;
            
            // Subscribe to ViewModel property changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Auto-load test files in debug mode
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            // Auto-load test input files
            // Try multiple possible paths to find test directory
            var possiblePaths = new[]
            {
                // From bin/Debug/net472/ to test/
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "test"),
                // From project root
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "test"),
                // Absolute path from solution directory
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "..", "..", "..", "..", "test")
            };

            string? testDir = null;
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    testDir = fullPath;
                    break;
                }
            }

            if (testDir != null && Directory.Exists(testDir))
            {
                // Load input.m3u8 and input1.m3u8 files
                var inputM3u8 = Path.Combine(testDir, "input.m3u8");
                var input1M3u8 = Path.Combine(testDir, "input1.m3u8");

                if (File.Exists(inputM3u8))
                {
                    var fullPath = Path.GetFullPath(inputM3u8);
                    if (!_viewModel.InputFiles.Contains(fullPath))
                    {
                        _viewModel.InputFiles.Add(fullPath);
                    }
                }

                if (File.Exists(input1M3u8))
                {
                    var fullPath = Path.GetFullPath(input1M3u8);
                    if (!_viewModel.InputFiles.Contains(fullPath))
                    {
                        _viewModel.InputFiles.Add(fullPath);
                    }
                }

                // Auto-set search directory to test_audio_files if exists
                var testAudioDir = Path.Combine(testDir, "test_audio_files");
                if (Directory.Exists(testAudioDir))
                {
                    _viewModel.SearchDirectory = Path.GetFullPath(testAudioDir);
                }
            }
#endif
        }

        private void InputFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string filePath)
            {
                _viewModel.PreviewFileCommand.Execute(filePath);
            }
        }

        private void RemoveSelectedFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (InputFilesListBox.SelectedItems.Count > 0)
            {
                var filesToRemove = InputFilesListBox.SelectedItems.Cast<string>().ToList();
                foreach (var file in filesToRemove)
                {
                    _viewModel.InputFiles.Remove(file);
                }
            }
        }

        private void OutputFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ViewModels.ProcessResultViewModel result)
            {
                _viewModel.SelectedProcessResult = result;
            }
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Subscribe to SelectedOutputFile changes to sync ListBox selection
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedProcessResult))
            {
                // Sync ListBox selection with ViewModel
                if (OutputFilesListBox != null && _viewModel.SelectedProcessResult != null)
                {
                    OutputFilesListBox.SelectedItem = _viewModel.SelectedProcessResult;
                }
            }
        }
    }
}

