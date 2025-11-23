using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Model API configuration for different model providers (e.g., OpenAI, DeepSeek, etc.)
/// </summary>
public partial class ModelApiConfig : ObservableObject, IEquatable<ModelApiConfig>
{
    /// <summary>
    /// Unique identifier for this configuration (GUID string)
    /// </summary>
    [ObservableProperty]
    private string _id = string.Empty;

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
    /// Whether this config is read-only (built-in configs from developer)
    /// </summary>
    [ObservableProperty]
    private bool _isReadOnly = false;

    /// <summary>
    /// Constructor for new instances and JSON deserialization
    /// If id is null or empty, generates a new GUID
    /// Other properties will be set via property setters by System.Text.Json during deserialization
    /// </summary>
    [JsonConstructor]
    public ModelApiConfig(string? id = null)
    {
        // Use provided ID from JSON, or generate GUID if not provided
        _id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
    }

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

    /// <summary>
    /// Compare two ModelApiConfig instances by Id
    /// </summary>
    public bool Equals(ModelApiConfig? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    /// <summary>
    /// Override Equals for object comparison
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelApiConfig);
    }

    /// <summary>
    /// Get hash code based on Id
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Create a clone of this config (with same Id)
    /// </summary>
    public ModelApiConfig Clone()
    {
        return new ModelApiConfig
        {
            Id = Id,
            ApiKey = ApiKey,
            ModelId = ModelId,
            BaseUrl = BaseUrl,
            Title = Title,
            IsReadOnly = IsReadOnly
        };
    }
}

