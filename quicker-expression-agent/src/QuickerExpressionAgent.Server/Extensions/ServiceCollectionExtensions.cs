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
        
        // Kernel
        services.AddSingleton<IKernelService, KernelService>();
        
        // Roslyn service
        services.AddSingleton<IRoslynExpressionService, RoslynExpressionService>();
        
        // Quicker service connector (connects to .Quicker project via pipe)
        // Must be registered before QuickerExpressionService as it's a dependency
        services.AddSingleton<QuickerServerClientConnector>();
        
        // Quicker expression service (connects to .Quicker project via pipe)
        services.AddSingleton<IQuickerExpressionService, QuickerExpressionService>();
        
        // Tool handler
        services.AddSingleton<IExpressionAgentToolHandler, ServerToolHandler>();
        
        // Plugin (registered after tool handler)
        services.AddSingleton(serviceProvider =>
        {
            var toolHandler = serviceProvider.GetRequiredService<IExpressionAgentToolHandler>();
            var roslynService = serviceProvider.GetRequiredService<IRoslynExpressionService>();
            var plugin = new ExpressionAgentPlugin(toolHandler, roslynService);
            
            // Register plugin to kernel
            var kernel = serviceProvider.GetRequiredService<IKernelService>().Kernel;
            var kernelPlugin = KernelPluginFactory.CreateFromObject(plugin, "ExpressionAgent");
            kernel.Plugins.Add(kernelPlugin);
            
            return plugin;
        });
        
        // Agent (registered after kernel and plugin)
        services.AddSingleton(serviceProvider =>
        {
            var kernel = serviceProvider.GetRequiredService<IKernelService>().Kernel;
            var roslynService = serviceProvider.GetRequiredService<IRoslynExpressionService>();
            var toolHandler = serviceProvider.GetRequiredService<IExpressionAgentToolHandler>();
            
            return new Agent.SemanticKernelExpressionAgent(kernel, roslynService, toolHandler);
        });
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        
        return services;
    }
}

