using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Desktop;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for managing MainWindow display and navigation
/// </summary>
public class MainWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindowService>? _logger;

    public MainWindowService(IServiceProvider serviceProvider, ILogger<MainWindowService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Show MainWindow and navigate to the specified page type
    /// </summary>
    /// <param name="pageType">Page type to navigate to</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool ShowAndNavigate(Type pageType)
    {
        try
        {
            // Get or create MainWindow
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }

            // Show and activate MainWindow
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Show();
            mainWindow.Activate();

            // Navigate to specified page
            var navigationService = _serviceProvider.GetRequiredService<Wpf.Ui.INavigationService>();
            return navigationService.Navigate(pageType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show MainWindow and navigate to {PageType}", pageType.Name);
            return false;
        }
    }

    /// <summary>
    /// Show MainWindow and navigate to the specified page type (generic version)
    /// </summary>
    /// <typeparam name="TPage">Page type to navigate to</typeparam>
    /// <returns>True if successful, false otherwise</returns>
    public bool ShowAndNavigate<TPage>() where TPage : class
    {
        return ShowAndNavigate(typeof(TPage));
    }

    /// <summary>
    /// Show MainWindow without navigation
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public bool Show()
    {
        try
        {
            // Get or create MainWindow
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }

            // Show and activate MainWindow
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Show();
            mainWindow.Activate();

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show MainWindow");
            return false;
        }
    }
}

