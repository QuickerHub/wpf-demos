using System.Windows;
using QuickerExpressionAgent.Quicker.ViewModels;

namespace QuickerExpressionAgent.Quicker;

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

