using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Desktop.ViewModels;
using QuickerExpressionAgent.Server.Services;

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
        
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<QuickerServiceTestViewModel>();
        services.AddTransient<ChatWindowViewModel>();
        
        // Register Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<QuickerServiceTestWindow>();
        services.AddTransient<ChatWindow>();
        
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

