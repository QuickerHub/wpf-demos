using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.Windows;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for managing a list of ModelApiConfig
/// </summary>
public partial class ApiConfigListViewModel : ObservableObject
{
    private readonly ApiConfigStorageService _storageService;
    private readonly CurrentApiConfigService _currentApiConfigService;

    [ObservableProperty]
    private ObservableCollection<ModelApiConfigItemViewModel> _configItems = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ModelApiConfigItemViewModel? _currentActiveConfig;

    public ApiConfigListViewModel(
        CurrentApiConfigService currentApiConfigService,
        ApiConfigStorageService storageService)
    {
        _storageService = storageService;
        _currentApiConfigService = currentApiConfigService;
        LoadConfigs();
        UpdateCurrentActiveConfig();
        
        // Listen for config changes
        _currentApiConfigService.CurrentConfigChanged += (s, e) => UpdateCurrentActiveConfig();
    }

    /// <summary>
    /// Update current active config indicator
    /// </summary>
    private void UpdateCurrentActiveConfig()
    {
        var current = _currentApiConfigService.CurrentConfig;
        if (current != null)
        {
            CurrentActiveConfig = ConfigItems.FirstOrDefault(item =>
                item.ApiKey == current.ApiKey &&
                item.ModelId == current.ModelId &&
                item.BaseUrl == current.BaseUrl);
        }
        else
        {
            CurrentActiveConfig = null;
        }
    }

    /// <summary>
    /// Load configurations from storage
    /// Note: Only loads user-configured configs from storage, does NOT include default config
    /// </summary>
    private void LoadConfigs()
    {
        // Only load user-configured configs from storage file
        // Default config is NOT included here to avoid exposing it in the API config management page
        var configs = _storageService.LoadConfigs();
        SetConfigs(configs);
        
        if (configs.Count == 0)
        {
            StatusMessage = "暂无配置，点击\"添加配置\"创建新配置";
        }
        else
        {
            StatusMessage = $"已加载 {configs.Count} 个配置";
        }
    }

    /// <summary>
    /// Add a new configuration with template selection
    /// </summary>
    [RelayCommand]
    private void AddConfig()
    {
        // Show template selection dialog
        var templates = ApiConfigTemplateService.GetTemplates();
        var templateWindow = new ApiConfigTemplateWindow(templates);
        
        if (templateWindow.ShowDialog() == true && templateWindow.SelectedTemplate != null)
        {
            // Create config from selected template
            var template = templateWindow.SelectedTemplate;
            var newConfig = new ModelApiConfig
            {
                ApiKey = template.Config.ApiKey,
                ModelId = template.Config.ModelId,
                BaseUrl = template.Config.BaseUrl
            };
            var newItem = new ModelApiConfigItemViewModel(newConfig);
            // Subscribe to save event to auto-save all configs
            newItem.ConfigSaved += (s, e) => AutoSaveConfigs();
            ConfigItems.Add(newItem);
            
            // Automatically start editing the newly added item
            newItem.BeginEdit();
            
            StatusMessage = $"已添加 {template.Name} 配置，请编辑 API Key 后保存";
        }
    }

    /// <summary>
    /// Remove a configuration
    /// </summary>
    [RelayCommand]
    private void RemoveConfig(ModelApiConfigItemViewModel item)
    {
        if (item != null)
        {
            ConfigItems.Remove(item);
        }
    }

    /// <summary>
    /// Get all configurations
    /// </summary>
    public List<ModelApiConfig> GetAllConfigs()
    {
        return ConfigItems.Select(item => item.GetConfig()).ToList();
    }

    /// <summary>
    /// Set configurations (for loading from storage)
    /// </summary>
    public void SetConfigs(IEnumerable<ModelApiConfig> configs)
    {
        ConfigItems.Clear();
        foreach (var config in configs)
        {
            var item = new ModelApiConfigItemViewModel(config);
            // Subscribe to save event to auto-save all configs
            item.ConfigSaved += (s, e) => AutoSaveConfigs();
            ConfigItems.Add(item);
        }
        UpdateCurrentActiveConfig();
    }

    /// <summary>
    /// Auto-save all configurations (called when individual config is saved)
    /// </summary>
    private void AutoSaveConfigs()
    {
        try
        {
            var configs = GetAllConfigs();
            _storageService.SaveConfigs(configs);
            _currentApiConfigService.ReloadConfigs();
            UpdateCurrentActiveConfig();
        }
        catch
        {
            // Ignore errors in auto-save (user can manually save if needed)
        }
    }

    /// <summary>
    /// Switch to a different API configuration
    /// </summary>
    [RelayCommand]
    private void SwitchToConfig(ModelApiConfigItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        try
        {
            var config = item.GetConfig();
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                StatusMessage = "请先配置 API Key";
                MessageBox.Show("请先配置 API Key 后再切换模型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _currentApiConfigService.SwitchConfig(config);
            StatusMessage = $"已切换到模型: {config.ModelId}";
            UpdateCurrentActiveConfig();
        }
        catch (Exception ex)
        {
            StatusMessage = $"切换模型失败: {ex.Message}";
            MessageBox.Show($"切换模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Save all configurations to storage
    /// </summary>
    [RelayCommand]
    private void SaveAll()
    {
        try
        {
            var configs = GetAllConfigs();
            _storageService.SaveConfigs(configs);
            _currentApiConfigService.ReloadConfigs();
            UpdateCurrentActiveConfig();
            StatusMessage = $"已保存 {configs.Count} 个配置";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

