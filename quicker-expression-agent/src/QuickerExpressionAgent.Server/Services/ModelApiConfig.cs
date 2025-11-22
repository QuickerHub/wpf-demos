namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Model API configuration for different model providers (e.g., OpenAI, DeepSeek, etc.)
/// </summary>
public class ModelApiConfig
{
    /// <summary>
    /// API key for the model provider
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model ID to use
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the API endpoint
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

