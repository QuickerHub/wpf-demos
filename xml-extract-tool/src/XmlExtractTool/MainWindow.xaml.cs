using System.Windows;
using XmlExtractTool.ViewModels;

namespace XmlExtractTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly XmlExtractViewModel _viewModel;

        public XmlExtractViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new XmlExtractViewModel();
            DataContext = this;
        }
    }
}
