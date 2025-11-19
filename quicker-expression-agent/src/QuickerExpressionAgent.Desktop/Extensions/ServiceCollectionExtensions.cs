using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        
        // Kernel
        services.AddSingleton<IKernelService, KernelService>();
        
        // Roslyn service
        services.AddSingleton<RoslynExpressionService>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning);
        });
        
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

