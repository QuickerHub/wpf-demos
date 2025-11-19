using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Implementation of IKernelService
/// </summary>
public class KernelService : IKernelService
{
    public Kernel Kernel { get; }

    public KernelService(IConfigurationService configurationService, ILogger<KernelService> logger)
    {
        var apiKey = configurationService.GetApiKey();
        var baseUrl = configurationService.GetBaseUrl();
        var modelId = configurationService.GetModelId();

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key not found. Please set it in appsettings.json or OPENAI_API_KEY environment variable.");
        }

        // Create kernel (same as desktop project)
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            endpoint: new Uri(baseUrl));

        Kernel = kernelBuilder.Build();
        
        logger.LogInformation("Semantic Kernel initialized with model: {ModelId}, endpoint: {BaseUrl}", modelId, baseUrl);
    }
}

