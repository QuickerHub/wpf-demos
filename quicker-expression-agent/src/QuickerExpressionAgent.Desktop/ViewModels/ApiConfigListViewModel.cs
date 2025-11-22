using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Desktop.Services;
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

    [ObservableProperty]
    private ObservableCollection<ModelApiConfigItemViewModel> _configItems = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ApiConfigListViewModel()
    {
        _storageService = new ApiConfigStorageService();
        LoadConfigs();
    }

    /// <summary>
    /// Load configurations from storage
    /// </summary>
    private void LoadConfigs()
    {
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
    /// Add a new configuration
    /// </summary>
    [RelayCommand]
    private void AddConfig()
    {
        var newConfig = new ModelApiConfig
        {
            ApiKey = string.Empty,
            ModelId = "deepseek-chat",
            BaseUrl = "https://api.deepseek.com/v1"
        };
        ConfigItems.Add(new ModelApiConfigItemViewModel(newConfig));
        StatusMessage = "已添加新配置，请编辑后保存";
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
            ConfigItems.Add(new ModelApiConfigItemViewModel(config));
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
            StatusMessage = $"已保存 {configs.Count} 个配置";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

