using System.IO;
using Microsoft.Extensions.Configuration;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Implementation of IConfigurationService
/// </summary>
public class ConfigurationService : IConfigurationService
{
    public IConfiguration Configuration { get; }

    public ConfigurationService()
    {
        // Load configuration (same as demo project)
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();
        
        var configPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
            Path.Combine(basePath, "appsettings.json")
        };

        var configBuilder = new ConfigurationBuilder();
        foreach (var path in configPaths)
        {
            if (File.Exists(path))
            {
                configBuilder.AddJsonFile(path, optional: true, reloadOnChange: true);
                break;
            }
        }

        Configuration = configBuilder
            .AddEnvironmentVariables()
            .Build();
    }

    public string GetApiKey()
    {
        return Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
    }

    public string GetBaseUrl()
    {
        return Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
    }

    public string GetModelId()
    {
        return Configuration["OpenAI:ModelId"] ?? "deepseek-chat";
    }
}

