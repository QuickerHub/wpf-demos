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
        // Load from environment variables only
        var configBuilder = new ConfigurationBuilder();
        Configuration = configBuilder
            .AddEnvironmentVariables()
            .Build();
    }

    public ModelApiConfig GetConfig()
    {
        // Priority: EmbeddedConfig -> Environment Variables -> Defaults
        string GetApiKey()
        {
            if (!string.IsNullOrEmpty(EmbeddedConfig.ApiKey))
                return EmbeddedConfig.ApiKey;
            var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            return !string.IsNullOrEmpty(envKey) ? envKey : "";
        }

        string GetBaseUrl()
        {
            if (!string.IsNullOrEmpty(EmbeddedConfig.BaseUrl))
                return EmbeddedConfig.BaseUrl;
            var envUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
            return !string.IsNullOrEmpty(envUrl) ? envUrl : "https://api.openai.com/v1";
        }

        string GetModelId()
        {
            if (!string.IsNullOrEmpty(EmbeddedConfig.ModelId))
                return EmbeddedConfig.ModelId;
            var envModelId = Environment.GetEnvironmentVariable("OPENAI_MODEL_ID");
            return !string.IsNullOrEmpty(envModelId) ? envModelId : "deepseek-chat";
        }

        return new ModelApiConfig
        {
            ApiKey = GetApiKey(),
            BaseUrl = GetBaseUrl(),
            ModelId = GetModelId()
        };
    }
}

