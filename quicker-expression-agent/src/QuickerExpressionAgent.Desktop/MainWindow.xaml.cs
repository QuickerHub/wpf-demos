using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions;

namespace QuickerExpressionAgent.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow, INavigationWindow
{
    public NavigationViewModel ViewModel { get; }

    public MainWindow(
        NavigationViewModel viewModel,
        Services.PageService pageService,
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IServiceProvider serviceProvider)
    {
        ViewModel = viewModel;
        DataContext = this;
        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(pageService);
        SetServiceProvider(serviceProvider);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(RootNavigation);
    }

    #region INavigationWindow methods

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(INavigationViewPageProvider pageProvider)
    {
        // In WPF-UI 4.0.3, SetPageService is called via SetServiceProvider
        // The page provider is set through the service provider
    }

    public void SetServiceProvider(IServiceProvider serviceProvider) => RootNavigation.SetServiceProvider(serviceProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    #endregion INavigationWindow methods

    /// <summary>
    /// Raises the closed event.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // Make sure that closing this window will begin the process of closing the application.
        Application.Current.Shutdown();
    }
}

