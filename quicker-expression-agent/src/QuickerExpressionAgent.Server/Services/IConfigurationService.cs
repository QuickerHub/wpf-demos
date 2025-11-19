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
    /// Gets OpenAI API key from configuration or environment variable
    /// </summary>
    string GetApiKey();
    
    /// <summary>
    /// Gets OpenAI base URL from configuration
    /// </summary>
    string GetBaseUrl();
    
    /// <summary>
    /// Gets OpenAI model ID from configuration
    /// </summary>
    string GetModelId();
}

