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
        // GetConfig() returns the first config from GetBuiltInConfigs()
        // Throws exception if no built-in configs available
        var builtInConfigs = GetBuiltInConfigs();
        
        if (builtInConfigs.Count == 0)
        {
            throw new InvalidOperationException("No built-in configurations available. Please ensure at least one API key (DEEPSEEK_API_KEY or GLM_API_KEY) is configured in .env file.");
        }
        
        return builtInConfigs[0];
    }

    public IReadOnlyList<ModelApiConfig> GetBuiltInConfigs()
    {
        // Return built-in configurations provided by developer
        // Use GLM API Key for GLM models, DeepSeek API Key for DeepSeek
        var glmApiKey = EmbeddedConfig.GlmApiKey;
        var deepseekApiKey = EmbeddedConfig.DeepSeekApiKey;
        
        // Fixed string IDs for built-in configs to ensure consistency
        const string Glm45ConfigId = "default-glm-4.5";
        const string Glm45AirConfigId = "default-glm-4.5-air";
        const string Glm46ConfigId = "default-glm-4.6";
        const string DeepSeekConfigId = "default-deepseek-chat";
        const string GlmBaseUrl = "https://open.bigmodel.cn/api/paas/v4";
        const string DeepSeekBaseUrl = "https://api.deepseek.com/v1";
        
        // GLM-4.5 config (use GLM API Key)
        var glm45Config = new ModelApiConfig(Glm45ConfigId)
        {
            ApiKey = glmApiKey,
            ModelId = "glm-4.5",
            BaseUrl = GlmBaseUrl,
            Title = "default:glm-4.5",
            IsReadOnly = true
        };
        
        // GLM-4.5-Air config (use GLM API Key)
        var glm45AirConfig = new ModelApiConfig(Glm45AirConfigId)
        {
            ApiKey = glmApiKey,
            ModelId = "glm-4.5-air",
            BaseUrl = GlmBaseUrl,
            Title = "default:glm-4.5-air",
            IsReadOnly = true
        };
        
        // GLM-4.6 config (use GLM API Key)
        var glm46Config = new ModelApiConfig(Glm46ConfigId)
        {
            ApiKey = glmApiKey,
            BaseUrl = GlmBaseUrl,
            ModelId = "glm-4.6",
            Title = "default:glm-4.6",
            IsReadOnly = true
        };
        
        // DeepSeek config (use DeepSeek API Key)
        var deepseekConfig = new ModelApiConfig(DeepSeekConfigId)
        {
            ApiKey = deepseekApiKey,
            BaseUrl = DeepSeekBaseUrl,
            ModelId = "deepseek-chat",
            Title = "default:deepseek-chat",
            IsReadOnly = true
        };
        
        // Return in order: glm-4.5-air, glm-4.5, glm-4.6, deepseek-chat
        return new List<ModelApiConfig> { glm45AirConfig, glm46Config, deepseekConfig }.AsReadOnly();
    }
}

