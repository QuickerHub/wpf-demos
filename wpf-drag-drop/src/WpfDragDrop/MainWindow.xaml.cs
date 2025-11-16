using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace WpfDragDrop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileDropControl_FilesDropped(object sender, FileDropRoutedEventArgs e)
        {
            // Update UI
            FileCountText.Text = $"收到了 {e.FileCount} 个文件";

            // Show file list
            var sb = new StringBuilder();
            sb.AppendLine($"共收到 {e.FileCount} 个文件：");
            sb.AppendLine();
            
            foreach (var filePath in e.FilePaths)
            {
                sb.AppendLine(filePath);
            }

            MessageBox.Show(sb.ToString(), "文件拖拽", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

