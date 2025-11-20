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
            throw new InvalidOperationException("OpenAI API key not found. Please set it in appsettings.json or OPENAI_API_KEY environment variable.");
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
    /// Creates a Kernel instance using AIModelConfig
    /// </summary>
    /// <param name="modelConfig">AI model configuration</param>
    /// <returns>Configured Kernel instance</returns>
    public static Kernel GetKernel(AIModelConfig modelConfig)
    {
        if (modelConfig == null)
        {
            throw new ArgumentNullException(nameof(modelConfig));
        }
        
        return GetKernel(modelConfig.ApiKey, modelConfig.BaseUrl, modelConfig.ModelId);
    }
    
    /// <summary>
    /// Creates a Kernel instance using IConfigurationService
    /// </summary>
    /// <param name="configurationService">Configuration service</param>
    /// <returns>Configured Kernel instance</returns>
    public static Kernel GetKernel(IConfigurationService configurationService)
    {
        var apiKey = configurationService.GetApiKey();
        var baseUrl = configurationService.GetBaseUrl();
        var modelId = configurationService.GetModelId();
        
        return GetKernel(apiKey, baseUrl, modelId);
    }
}


/// <summary>
/// AI model configuration
/// </summary>
public class AIModelConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "";
    public string ModelId { get; set; } = "";
}

