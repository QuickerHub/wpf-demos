using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using QuickerExpressionAgent.Server.Generated;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Implementation of IConfigurationService
/// </summary>
public class ConfigurationService : IConfigurationService
{
    public IConfiguration Configuration { get; }

    public ConfigurationService()
    {
        var configBuilder = new ConfigurationBuilder();
        
        // First, add embedded config (generated at compile time from .env)
        // Only ApiKey is embedded, ModelId and BaseUrl use default values
        var embeddedApiKey = EmbeddedConfig.ApiKey;
        
        if (!string.IsNullOrEmpty(embeddedApiKey))
        {
            // Add embedded config as in-memory configuration
            var embeddedConfig = new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = embeddedApiKey
            };
            configBuilder.AddInMemoryCollection(embeddedConfig);
        }
        
        // Then load from file system (for development or fallback)
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();
        
        var configPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
            Path.Combine(basePath, "appsettings.json")
        };

        foreach (var path in configPaths)
        {
            if (File.Exists(path))
            {
                configBuilder.AddJsonFile(path, optional: true, reloadOnChange: false);
            }
        }

        Configuration = configBuilder
            .AddEnvironmentVariables()
            .Build();
    }

    public string GetApiKey() => Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

    public string GetBaseUrl() => Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

    public string GetModelId() => Configuration["OpenAI:ModelId"] ?? "deepseek-chat";
}

