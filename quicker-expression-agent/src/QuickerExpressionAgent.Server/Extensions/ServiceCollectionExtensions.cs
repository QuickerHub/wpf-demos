using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Plugins;
using QuickerExpressionAgent.Server.Services;
using Serilog;
using System.IO;

namespace QuickerExpressionAgent.Server.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Serilog file logging
    /// </summary>
    public static void ConfigureSerilogLogging()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var logsDirectory = Path.Combine(appDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var logFilePath = Path.Combine(logsDirectory, "app-.log");
        
#if DEBUG
        // Clear log files in Debug mode - delete all app-*.log files
        var logDirectory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
        {
            var logFiles = Directory.GetFiles(logDirectory, "app-*.log");
            foreach (var logFile in logFiles)
            {
                try
                {
                    File.Delete(logFile);
                }
                catch
                {
                    // Ignore errors when deleting log files
                }
            }
        }
#endif
        
        // Configure Serilog
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug() // Set to Debug to capture ChatUpdate JSON
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}",
                rollingInterval: RollingInterval.Day);
        
        Log.Logger = loggerConfiguration.CreateLogger();
    }
    
    /// <summary>
    /// Adds all server application services to the service collection
    /// </summary>
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        // Logging - Add Serilog to Microsoft.Extensions.Logging
        services.AddLogging(c => c.AddSerilog());
        
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
            // Use GetRequiredService to ensure logger is not null
            var logger = serviceProvider.GetRequiredService<ILogger<Agent.ExpressionAgent>>();
            
            return new Agent.ExpressionAgent(kernel, executor, toolHandler, logger);
        });
        
        // Logging is configured in Program.cs via ConfigureLogging
        
        return services;
    }
}

