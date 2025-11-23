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
        BeginEdit();
    }

    /// <summary>
    /// Begin editing mode (public method for external use)
    /// </summary>
    public void BeginEdit()
    {
        _tempApiKey = ApiKey;
        _tempModelId = ModelId;
        _tempBaseUrl = BaseUrl;
        IsEditing = true;
    }

    /// <summary>
    /// Event raised when config is saved
    /// </summary>
    public event EventHandler? ConfigSaved;

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
        
        // Notify that config was saved
        ConfigSaved?.Invoke(this, EventArgs.Empty);
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
    /// Get the current ModelApiConfig (with current property values)
    /// </summary>
    public ModelApiConfig GetConfig()
    {
        // Return a new config with current property values (not the underlying _config)
        // This ensures we get the latest values even if not saved yet
        return new ModelApiConfig
        {
            ApiKey = ApiKey,
            ModelId = ModelId,
            BaseUrl = BaseUrl
        };
    }
}

