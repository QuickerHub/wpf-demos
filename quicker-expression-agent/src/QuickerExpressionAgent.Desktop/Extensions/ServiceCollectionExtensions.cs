using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Desktop.Pages;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.ViewModels;
using QuickerExpressionAgent.Server.Services;
using WindowAttach.Services;
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
        
        // Config Service (unified configuration management)
        services.AddSingleton<ConfigService>();
        
        // Expression executor
        services.AddSingleton<ExpressionExecutor>();
        
        // Communication services (Desktop <-> Quicker)
        services.AddCommunicationServices();

        // Window attach service
        services.AddSingleton<WindowAttachService>();

        // Tray icon service
        services.AddSingleton<NotifyIconService>();

        // MainWindow service
        services.AddSingleton<MainWindowService>();

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
               services.AddTransient<ExpressionGeneratorPageViewModel>();
               services.AddTransient<QuickerServiceTestViewModel>();
               services.AddTransient<ChatWindowViewModel>();
               services.AddTransient<InfoPageViewModel>();
               // ApiConfigListViewModel needs IConfigurationService to provide AvailableApiConfigs
               services.AddSingleton<ApiConfigListViewModel>(provider => 
                   new ApiConfigListViewModel(
                       provider.GetRequiredService<ConfigService>(),
                       provider.GetRequiredService<IConfigurationService>())); // Singleton - only this ViewModel controls API config storage
               services.AddSingleton<NavigationViewModel>();
               services.AddTransient<ExpressionAgentViewModel>();
        
        // Register Pages as Singleton to maintain state when navigating
        services.AddSingleton<ExpressionGeneratorPage>();
        services.AddSingleton<ApiConfigPage>();
        services.AddSingleton<TestPage>();
        services.AddSingleton<InfoPage>();
        
        // Register Windows
        services.AddSingleton<MainWindow>();
        services.AddTransient<QuickerServiceTestWindow>();
        services.AddTransient<ChatWindow>();
        
        return services;
    }
    
    /// <summary>
    /// Adds communication services for Desktop project
    /// Registers:
    /// - QuickerServerClientConnector: Desktop calls Quicker
    /// - DesktopServiceServer: Quicker calls Desktop
    /// </summary>
    public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
    {
        // Desktop calls Quicker (client connector)
        services.AddSingleton<QuickerServerClientConnector>();
        services.AddHostedService(provider => provider.GetRequiredService<QuickerServerClientConnector>());

        // Quicker calls Desktop (server)
        services.AddSingleton<DesktopServiceImplementation>();
        services.AddSingleton<DesktopServiceServer>();
        services.AddHostedService(provider => provider.GetRequiredService<DesktopServiceServer>());

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

