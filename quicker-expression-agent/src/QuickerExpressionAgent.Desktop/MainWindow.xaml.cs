using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.ViewModels;
using QuickerExpressionAgent.Desktop.Services;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Tray.Controls;

namespace QuickerExpressionAgent.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow, INavigationWindow
{
    public NavigationViewModel ViewModel { get; }
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(
        NavigationViewModel viewModel,
        Services.PageService pageService,
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IServiceProvider serviceProvider)
    {
        ViewModel = viewModel;
        DataContext = this;
        _serviceProvider = serviceProvider;
        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(pageService);
        SetServiceProvider(serviceProvider);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(RootNavigation);
        
        // Setup NotifyIcon menu after window is loaded
        Loaded += (s, e) =>
        {
            var notifyIconService = _serviceProvider.GetService<NotifyIconService>();
            if (notifyIconService != null && TheNotifyIcon != null)
            {
                TheNotifyIcon.Menu = notifyIconService.GetMenu();
            }
        };
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
    /// Raises the closing event.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Cancel the close operation and hide window to tray instead
        // Only shutdown if explicitly requested (e.g., from tray menu)
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}

