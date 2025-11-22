using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Desktop.Pages;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.ViewModels;
using QuickerExpressionAgent.Server.Services;
using Wpf.Ui;

namespace QuickerExpressionAgent.Desktop.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all desktop application services to the service collection
    /// </summary>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Expression executor
        services.AddSingleton<ExpressionExecutor>();
        
        // Quicker service connector (reuse from Server project)
        services.AddSingleton<QuickerServerClientConnector>();
        services.AddHostedService(provider => provider.GetRequiredService<QuickerServerClientConnector>());

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning);
        });
        
        // WPF-UI Services
        services.AddSingleton<Services.PageService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        
        // Register PageService as INavigationViewPageProvider for WPF-UI 4.0.3
        services.AddSingleton<Wpf.Ui.Abstractions.INavigationViewPageProvider>(provider => provider.GetRequiredService<Services.PageService>());
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<QuickerServiceTestViewModel>();
        services.AddTransient<ChatWindowViewModel>();
        services.AddTransient<ApiConfigListViewModel>();
        services.AddSingleton<NavigationViewModel>();
        
        // Register Pages
        services.AddTransient<ExpressionGeneratorPage>();
        services.AddTransient<ApiConfigPage>();
        services.AddTransient<TestPage>();
        
        // Register Windows
        services.AddSingleton<MainWindow>();
        services.AddTransient<QuickerServiceTestWindow>();
        services.AddTransient<ChatWindow>();
        services.AddTransient<ApiConfigListWindow>();
        
        return services;
    }
    
    /// <summary>
    /// Gets a logger for the specified type
    /// </summary>
    public static ILogger<T> GetLogger<T>(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}

