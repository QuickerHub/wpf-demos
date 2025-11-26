using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Quicker.Services;

namespace QuickerExpressionAgent.Quicker.Extensions;

/// <summary>
/// Extension methods for configuring services in Quicker project
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a hosted service that runs on UI thread
    /// </summary>
    /// <typeparam name="T">The type of hosted service to add</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHostedServiceOnUiThread<T>(this IServiceCollection services)
        where T : class, IHostedService
    {
        services.AddSingleton<T>();
        services.AddHostedService(serviceProvider =>
        {
            var innerService = serviceProvider.GetRequiredService<T>();
            var logger = serviceProvider.GetService<ILogger<UiThreadHostedService>>();
            return new UiThreadHostedService(innerService, logger);
        });
        return services;
    }

    /// <summary>
    /// Adds a hosted service that runs on UI thread using a factory function
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="implementationFactory">Factory function to create the hosted service</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHostedServiceOnUiThread(
        this IServiceCollection services,
        Func<IServiceProvider, IHostedService> implementationFactory)
    {
        services.AddHostedService(serviceProvider =>
        {
            var innerService = implementationFactory(serviceProvider);
            var logger = serviceProvider.GetService<ILogger<UiThreadHostedService>>();
            return new UiThreadHostedService(innerService, logger);
        });
        return services;
    }
    /// <summary>
    /// Adds communication services for Quicker project
    /// Registers:
    /// - QuickerServiceServer: Desktop calls Quicker
    /// - DesktopServiceClientConnector: Quicker calls Desktop
    /// - ActiveWindowService: Monitors active window changes
    /// </summary>
    public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
    {
        // Desktop calls Quicker (server)
        services.AddSingleton<QuickerServiceImplementation>();
        services.AddSingleton<IQuickerService>(s => s.GetRequiredService<QuickerServiceImplementation>());
        services.AddSingleton<QuickerServiceServer>();
        services.AddHostedService(s => s.GetRequiredService<QuickerServiceServer>());

        // Quicker calls Desktop (client connector)
        services.AddSingleton<DesktopServiceClientConnector>();
        services.AddHostedService(s => s.GetRequiredService<DesktopServiceClientConnector>());

        // Active window monitoring service (runs on UI thread)
        services.AddHostedServiceOnUiThread<ActiveWindowService>();

        return services;
    }
}

