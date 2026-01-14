using System.Windows;
using QuickerScreenshot.ViewModels;

namespace QuickerScreenshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainWindowViewModel();
            DataContext = this; // Set DataContext to this, not ViewModel
        }
    }
}
