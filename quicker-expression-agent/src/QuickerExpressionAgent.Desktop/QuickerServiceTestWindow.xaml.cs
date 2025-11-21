using QuickerExpressionAgent.Desktop.ViewModels;
using System.Windows;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for QuickerServiceTestWindow.xaml
    /// </summary>
    public partial class QuickerServiceTestWindow : Window
    {
        public QuickerServiceTestViewModel ViewModel { get; set; }

        public QuickerServiceTestWindow(QuickerServiceTestViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = this;
        }
    }
}

