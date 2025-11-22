using System.Windows.Controls;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop.Pages;

/// <summary>
/// Interaction logic for ApiConfigPage.xaml
/// </summary>
public partial class ApiConfigPage : Page
{
    public ApiConfigListViewModel ViewModel { get; }

    public ApiConfigPage(ApiConfigListViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;
    }
}

