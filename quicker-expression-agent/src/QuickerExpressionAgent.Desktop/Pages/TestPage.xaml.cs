using System.Windows.Controls;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop.Pages;

/// <summary>
/// Interaction logic for TestPage.xaml
/// </summary>
public partial class TestPage : Page
{
    public QuickerServiceTestViewModel ViewModel { get; }

    public TestPage(QuickerServiceTestViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;
    }
}

