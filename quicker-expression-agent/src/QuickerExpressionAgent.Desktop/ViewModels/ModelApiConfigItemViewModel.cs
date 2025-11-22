using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for a single ModelApiConfig item in the list
/// </summary>
public partial class ModelApiConfigItemViewModel : ObservableObject
{
    private readonly ModelApiConfig _config;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private bool _isEditing = false;

    // Temporary values for editing
    private string _tempApiKey = string.Empty;
    private string _tempModelId = string.Empty;
    private string _tempBaseUrl = string.Empty;

    public ModelApiConfigItemViewModel(ModelApiConfig config)
    {
        _config = config;
        ApiKey = config.ApiKey;
        ModelId = config.ModelId;
        BaseUrl = config.BaseUrl;
    }

    /// <summary>
    /// Start editing - save current values to temp
    /// </summary>
    [RelayCommand]
    private void StartEdit()
    {
        _tempApiKey = ApiKey;
        _tempModelId = ModelId;
        _tempBaseUrl = BaseUrl;
        IsEditing = true;
    }

    /// <summary>
    /// Save changes - update config and exit edit mode
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        _config.ApiKey = ApiKey;
        _config.ModelId = ModelId;
        _config.BaseUrl = BaseUrl;
        IsEditing = false;
    }

    /// <summary>
    /// Cancel editing - restore temp values
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        ApiKey = _tempApiKey;
        ModelId = _tempModelId;
        BaseUrl = _tempBaseUrl;
        IsEditing = false;
    }

    /// <summary>
    /// Get the underlying ModelApiConfig
    /// </summary>
    public ModelApiConfig GetConfig() => _config;
}

