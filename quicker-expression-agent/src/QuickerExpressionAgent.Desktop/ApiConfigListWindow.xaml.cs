using QuickerExpressionAgent.Desktop.ViewModels;
using System.Windows;

namespace QuickerExpressionAgent.Desktop;

/// <summary>
/// Interaction logic for ApiConfigListWindow.xaml
/// </summary>
public partial class ApiConfigListWindow : Window
{
    public ApiConfigListViewModel ViewModel { get; }

    public ApiConfigListWindow()
    {
        InitializeComponent();
        ViewModel = new ApiConfigListViewModel();
        DataContext = this;
    }
}

