using System.Windows;

namespace VscodeTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenSelectedFileButton_Click(object sender, RoutedEventArgs e)
        {
            VscodeFileOpener.TryOpenFileFromClipboard();
        }
    }
}

