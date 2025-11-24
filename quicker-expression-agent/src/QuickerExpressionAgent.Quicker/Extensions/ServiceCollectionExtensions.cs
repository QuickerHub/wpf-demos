using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Quicker.Services;

namespace QuickerExpressionAgent.Quicker.Extensions;

/// <summary>
/// Extension methods for configuring services in Quicker project
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds communication services for Quicker project
    /// Registers:
    /// - QuickerServiceServer: Desktop calls Quicker
    /// - DesktopServiceClientConnector: Quicker calls Desktop
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

        return services;
    }
}

