using System.Windows.Controls;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop.Pages;

/// <summary>
/// Interaction logic for InfoPage.xaml
/// </summary>
public partial class InfoPage : Page
{
    public InfoPageViewModel ViewModel { get; }

    public InfoPage(InfoPageViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;
    }
}

