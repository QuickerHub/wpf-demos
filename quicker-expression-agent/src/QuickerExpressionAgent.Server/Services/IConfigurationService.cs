using Microsoft.Extensions.Configuration;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Service for loading and accessing application configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the configuration instance
    /// </summary>
    IConfiguration Configuration { get; }
    
    /// <summary>
    /// Gets the complete API configuration (ApiKey, BaseUrl, ModelId)
    /// Priority: EmbeddedConfig -> Environment Variables -> Defaults
    /// </summary>
    ModelApiConfig GetConfig();

    /// <summary>
    /// Gets all built-in configurations provided by developer (read-only)
    /// </summary>
    IReadOnlyList<ModelApiConfig> GetBuiltInConfigs();
}

