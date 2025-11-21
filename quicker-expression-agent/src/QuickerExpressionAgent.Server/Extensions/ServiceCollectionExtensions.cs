using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all server application services to the service collection
    /// </summary>
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Expression executor
        services.AddSingleton<IExpressionExecutor, ExpressionExecutor>();
        
        // Quicker service connector (connects to .Quicker project via pipe)
        // Register as singleton and hosted service (use same instance)
        services.AddSingleton<QuickerServerClientConnector>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<QuickerServerClientConnector>());
        
        // Tool handler
        services.AddSingleton<IExpressionAgentToolHandler, ServerToolHandler>();
        
        // Agent (registered after kernel and tool handler)
        services.AddSingleton(serviceProvider =>
        {
            var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
            var kernel = KernelService.GetKernel(configurationService);
            var executor = serviceProvider.GetRequiredService<IExpressionExecutor>();
            var toolHandler = serviceProvider.GetRequiredService<IExpressionAgentToolHandler>();
            
            return new Agent.ExpressionAgent(kernel, executor, toolHandler);
        });
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        
        return services;
    }
}

