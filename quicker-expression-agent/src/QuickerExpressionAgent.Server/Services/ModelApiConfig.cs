using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Model API configuration for different model providers (e.g., OpenAI, DeepSeek, etc.)
/// </summary>
public partial class ModelApiConfig : ObservableObject
{
    /// <summary>
    /// API key for the model provider
    /// </summary>
    [ObservableProperty]
    private string _apiKey = string.Empty;

    /// <summary>
    /// Model ID to use
    /// </summary>
    [ObservableProperty]
    private string _modelId = string.Empty;

    /// <summary>
    /// Base URL for the API endpoint
    /// </summary>
    [ObservableProperty]
    private string _baseUrl = string.Empty;

    /// <summary>
    /// Short title for ComboBox display (lowercase, short)
    /// </summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Visual title for display (auto-generated from Title or ModelId)
    /// </summary>
    public string VisualTitle => string.IsNullOrWhiteSpace(Title) ? ModelId : Title;

    /// <summary>
    /// Called when Title changes - notify VisualTitle change
    /// </summary>
    partial void OnTitleChanged(string value)
    {
        OnPropertyChanged(nameof(VisualTitle));
    }

    /// <summary>
    /// Called when ModelId changes - notify VisualTitle change
    /// </summary>
    partial void OnModelIdChanged(string value)
    {
        OnPropertyChanged(nameof(VisualTitle));
    }
}

