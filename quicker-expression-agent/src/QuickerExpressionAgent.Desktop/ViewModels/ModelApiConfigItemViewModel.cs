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
    private readonly ApiConfigListViewModel _parentViewModel;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _modelId = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isEditing = false;

    /// <summary>
    /// Whether this config is read-only (built-in configs from developer)
    /// </summary>
    public bool IsReadOnly => _config.IsReadOnly;

    // Snapshot of values when editing starts
    private ModelApiConfig? _editSnapshot;

    public ModelApiConfigItemViewModel(ModelApiConfig config, ApiConfigListViewModel parentViewModel)
    {
        _config = config;
        _parentViewModel = parentViewModel;
        ApiKey = config.ApiKey;
        ModelId = config.ModelId;
        BaseUrl = config.BaseUrl;
        Title = config.Title;
    }

    /// <summary>
    /// Start editing this item
    /// </summary>
    [RelayCommand]
    private void StartEdit()
    {
        _parentViewModel.StartEditItem(this);
    }

    /// <summary>
    /// Begin editing mode (called by parent ViewModel)
    /// </summary>
    public void BeginEdit()
    {
        // Save current state as snapshot using Clone pattern
        _editSnapshot = GetCurrentState();
        IsEditing = true;
    }

    /// <summary>
    /// Get current state as a ModelApiConfig snapshot
    /// </summary>
    private ModelApiConfig GetCurrentState()
    {
        return new ModelApiConfig
        {
            Id = _config.Id,
            ApiKey = ApiKey,
            ModelId = ModelId,
            BaseUrl = BaseUrl,
            Title = Title
        };
    }

    /// <summary>
    /// Restore state from snapshot
    /// </summary>
    private void RestoreFromSnapshot()
    {
        if (_editSnapshot != null)
        {
            ApiKey = _editSnapshot.ApiKey;
            ModelId = _editSnapshot.ModelId;
            BaseUrl = _editSnapshot.BaseUrl;
            Title = _editSnapshot.Title;
        }
    }

    /// <summary>
    /// Save changes
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        _parentViewModel.SaveEditItem(this);
    }

    /// <summary>
    /// Save changes to underlying config (called by parent ViewModel)
    /// </summary>
    public void SaveChanges()
    {
        _config.ApiKey = ApiKey;
        _config.ModelId = ModelId;
        _config.BaseUrl = BaseUrl;
        _config.Title = Title;
        IsEditing = false;
    }

    /// <summary>
    /// Cancel editing
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _parentViewModel.CancelEditItem(this);
    }

    /// <summary>
    /// Cancel editing and restore original values (called by parent ViewModel)
    /// </summary>
    public void CancelEdit()
    {
        RestoreFromSnapshot();
        _editSnapshot = null;
        IsEditing = false;
    }

    /// <summary>
    /// Remove this configuration
    /// </summary>
    [RelayCommand]
    private void Remove()
    {
        _parentViewModel.RemoveConfig(this);
    }

    /// <summary>
    /// Set this configuration as default
    /// </summary>
    [RelayCommand]
    private void SetAsDefault()
    {
        _parentViewModel.SetAsDefaultConfig(this);
    }

    /// <summary>
    /// Get the current ModelApiConfig (with current property values)
    /// </summary>
    public ModelApiConfig GetConfig()
    {
        // Return the underlying config with updated property values
        // This preserves the Id and ensures we get the latest values
        _config.ApiKey = ApiKey;
        _config.ModelId = ModelId;
        _config.BaseUrl = BaseUrl;
        _config.Title = Title;
        return _config;
    }
}

