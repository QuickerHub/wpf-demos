using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.ViewModels;
using System.Windows;

namespace QuickerExpressionAgent.Desktop;

/// <summary>
/// Interaction logic for ApiConfigListWindow.xaml
/// </summary>
public partial class ApiConfigListWindow : System.Windows.Window
{
    public ApiConfigListViewModel ViewModel { get; }

    public ApiConfigListWindow(ApiConfigListViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = this;
        
        // Auto-save when window closes
        Closing += ApiConfigListWindow_Closing;
    }

    private void ApiConfigListWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Auto-save all configurations when window closes
        try
        {
            ViewModel.SaveAllCommand.Execute(null);
        }
        catch
        {
            // Ignore errors during auto-save
        }
    }
}

