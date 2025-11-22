using Microsoft.SemanticKernel;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Static service for creating Kernel instances
/// </summary>
public static class KernelService
{
    /// <summary>
    /// Creates a Kernel instance with the specified configuration
    /// </summary>
    /// <param name="apiKey">OpenAI API key</param>
    /// <param name="baseUrl">OpenAI base URL</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>Configured Kernel instance</returns>
    public static Kernel GetKernel(string apiKey, string baseUrl, string modelId)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key not found. Please set it via one of the following methods:\n" +
                "1. Embedded config (compile-time from .env file)\n" +
                "2. OPENAI_API_KEY environment variable");
        }

        // Create kernel
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(baseUrl));

        return kernelBuilder.Build();
    }
    
    /// <summary>
    /// Creates a Kernel instance using IConfigurationService
    /// </summary>
    /// <param name="configurationService">Configuration service</param>
    /// <returns>Configured Kernel instance</returns>
    public static Kernel GetKernel(IConfigurationService configurationService)
    {
        var config = configurationService.GetConfig();
        return GetKernel(config);
    }
    
    /// <summary>
    /// Creates a Kernel instance using ModelApiConfig
    /// </summary>
    /// <param name="apiConfig">Model API configuration</param>
    /// <returns>Configured Kernel instance</returns>
    public static Kernel GetKernel(ModelApiConfig apiConfig)
    {
        if (apiConfig == null)
        {
            throw new ArgumentNullException(nameof(apiConfig));
        }
        
        return GetKernel(apiConfig.ApiKey, apiConfig.BaseUrl, apiConfig.ModelId);
    }
}

