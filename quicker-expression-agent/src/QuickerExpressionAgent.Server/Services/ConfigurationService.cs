using Microsoft.Extensions.Configuration;
using QuickerExpressionAgent.Server.Generated;
using System.Collections.Generic;

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

    public IReadOnlyList<ModelApiConfig> GetBuiltInConfigs()
    {
        // Return built-in configurations provided by developer
        var apiKey = EmbeddedConfig.ApiKey;
        
        // Fixed string IDs for built-in configs to ensure consistency
        const string Glm45ConfigId = "default-glm-4.5";
        const string Glm45AirConfigId = "default-glm-4.5-air";
        const string DefaultConfigId = "default-glm-4.6";
        const string GlmBaseUrl = "https://open.bigmodel.cn/api/paas/v4";
        
        // GLM-4.5 config
        var glm45Config = new ModelApiConfig(Glm45ConfigId)
        {
            ApiKey = apiKey,
            ModelId = "glm-4.5",
            BaseUrl = GlmBaseUrl,
            Title = "default:glm-4.5",
            IsReadOnly = true
        };
        
        // GLM-4.5-Air config
        var glm45AirConfig = new ModelApiConfig(Glm45AirConfigId)
        {
            ApiKey = apiKey,
            ModelId = "glm-4.5-air",
            BaseUrl = GlmBaseUrl,
            Title = "default:glm-4.5-air",
            IsReadOnly = true
        };
        
        // GLM-4.6 config
        var glm46Config = new ModelApiConfig(DefaultConfigId)
        {
            ApiKey = apiKey,
            BaseUrl = GlmBaseUrl,
            ModelId = "glm-4.6",
            Title = "default:glm-4.6",
            IsReadOnly = true
        };
        
        // Return in order: glm-4.5, glm-4.5-air, glm-4.6
        return new List<ModelApiConfig> { glm45Config, glm45AirConfig, glm46Config }.AsReadOnly();
    }
}

