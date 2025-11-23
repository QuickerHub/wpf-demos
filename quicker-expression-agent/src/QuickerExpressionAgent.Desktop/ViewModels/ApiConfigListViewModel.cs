using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.Windows;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for managing a list of ModelApiConfig (Singleton)
/// Only this ViewModel can control API config storage
/// </summary>
public partial class ApiConfigListViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly IConfigurationService _configurationService;

    /// <summary>
    /// Get API config storage (singleton)
    /// </summary>
    private ApiConfigStorage Storage => _configService.GetConfig<ApiConfigStorage>();

    /// <summary>
    /// Observable collection of config ViewModels for UI binding (user configs only, editable)
    /// </summary>
    public ObservableCollection<ModelApiConfigItemViewModel> Configs { get; } = new();

    /// <summary>
    /// Observable collection of built-in config ViewModels for UI binding (read-only)
    /// </summary>
    public ObservableCollection<ModelApiConfigItemViewModel> BuiltInConfigs { get; } = new();

    /// <summary>
    /// Available API configurations for selection (includes embedded default config and user configs)
    /// Automatically updated when Configs change
    /// </summary>
    public ObservableCollection<ModelApiConfig> AvailableApiConfigs { get; } = new();

    /// <summary>
    /// Default config to use (user-set default or first built-in config)
    /// Returns the actual config to use for agent creation
    /// </summary>
    public ModelApiConfig? DefaultConfig
    {
        get
        {
            var builtInConfigs = _configurationService.GetBuiltInConfigs();
            
            // Priority 1: Use saved default config if it exists and is valid
            if (Storage.DefaultConfig != null && !string.IsNullOrWhiteSpace(Storage.DefaultConfig.ApiKey))
            {
                // Check if saved default config is a built-in config
                var matchingBuiltIn = builtInConfigs.FirstOrDefault(c => c.Id == Storage.DefaultConfig.Id);
                if (matchingBuiltIn != null)
                {
                    return matchingBuiltIn;
                }
                
                // Otherwise, return the saved user config
                return Storage.DefaultConfig;
            }
            
            // Priority 2: Use first built-in config if available
            if (builtInConfigs.Count > 0)
            {
                return builtInConfigs[0];
            }
            
            return null;
        }
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Available API configurations for default selection (same as AvailableApiConfigs)
    /// </summary>
    public ObservableCollection<ModelApiConfig> AvailableDefaultConfigs => AvailableApiConfigs;

    /// <summary>
    /// Selected default API configuration
    /// </summary>
    [ObservableProperty]
    private ModelApiConfig? _selectedDefaultConfig;

    /// <summary>
    /// Currently editing config ViewModel
    /// </summary>
    [ObservableProperty]
    private ModelApiConfigItemViewModel? _editingConfig;

    public ApiConfigListViewModel(
        ConfigService configService,
        IConfigurationService configurationService)
    {
        _configService = configService;
        _configurationService = configurationService;
        
        // Listen to storage changes
        Storage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ApiConfigStorage.DefaultConfig))
            {
                LoadDefaultConfigs();
                OnPropertyChanged(nameof(DefaultConfig));
            }
        };
        
        // Listen to Configs collection changes to update AvailableApiConfigs
        Configs.CollectionChanged += (s, e) =>
        {
            UpdateAvailableApiConfigs();
            OnPropertyChanged(nameof(DefaultConfig));
            LoadDefaultConfigs(); // Update SelectedDefaultConfig when configs change
        };
        
        // Load configs from storage
        LoadConfigs();
        UpdateStatusMessage();
        LoadDefaultConfigs();
        UpdateAvailableApiConfigs();
    }

    /// <summary>
    /// Load configs from Storage and create ViewModels
    /// </summary>
    private void LoadConfigs()
    {
        Configs.Clear();
        foreach (var config in Storage.Configs)
        {
            var viewModel = new ModelApiConfigItemViewModel(config, this);
            Configs.Add(viewModel);
        }
    }

    /// <summary>
    /// Save configs from ViewModels to Storage
    /// </summary>
    private void SaveConfigs()
    {
        Storage.Configs = Configs.Select(vm => vm.GetConfig()).ToList();
        _configService.SaveConfig<ApiConfigStorage>();
    }

    /// <summary>
    /// Update status message based on config count
    /// </summary>
    private void UpdateStatusMessage()
    {
        StatusMessage = Configs.Count == 0 
            ? "暂无配置，点击\"添加配置\"创建新配置" 
            : $"已加载 {Configs.Count} 个配置";
    }

    /// <summary>
    /// Load available configurations for default selection
    /// </summary>
    private void LoadDefaultConfigs()
    {
        SelectedDefaultConfig = Storage.DefaultConfig != null
            ? AvailableApiConfigs.FirstOrDefault(c => c.Id == Storage.DefaultConfig.Id)
            : AvailableApiConfigs.FirstOrDefault();
    }

    /// <summary>
    /// Update AvailableApiConfigs collection (includes built-in configs and user configs)
    /// Also updates BuiltInConfigs collection for UI display
    /// </summary>
    private void UpdateAvailableApiConfigs()
    {
        var builtInConfigs = _configurationService.GetBuiltInConfigs();
        
        AvailableApiConfigs.Clear();
        BuiltInConfigs.Clear();
        
        // Add built-in configs first (read-only)
        foreach (var builtInConfig in builtInConfigs)
        {
            var displayConfig = builtInConfig.Clone();
            displayConfig.IsReadOnly = true; // Ensure read-only flag is set
            AvailableApiConfigs.Add(displayConfig);
            
            // Create ViewModel for UI display
            var viewModel = new ModelApiConfigItemViewModel(displayConfig, this);
            BuiltInConfigs.Add(viewModel);
        }
        
        // Add user-configured configs (editable)
        foreach (var vm in Configs)
        {
            AvailableApiConfigs.Add(vm.GetConfig());
        }
    }


    /// <summary>
    /// Get the actual config to use (not the display config) based on selected config
    /// This method is provided for ExpressionAgentViewModel to convert display config to actual config
    /// </summary>
    /// <param name="selectedConfig">Selected config from UI</param>
    /// <returns>Actual config to use for agent creation</returns>
    public ModelApiConfig? GetActualConfig(ModelApiConfig? selectedConfig)
    {
        if (selectedConfig == null) return null;
        
        // If selected config is a built-in config (read-only), find the original built-in config
        if (selectedConfig.IsReadOnly)
        {
            var builtInConfigs = _configurationService.GetBuiltInConfigs();
            var matchingBuiltIn = builtInConfigs.FirstOrDefault(c => c.Id == selectedConfig.Id);
            if (matchingBuiltIn != null)
            {
                return matchingBuiltIn;
            }
        }

        return selectedConfig;
    }

    /// <summary>
    /// Set a configuration as default (called by item ViewModel)
    /// </summary>
    public void SetAsDefaultConfig(ModelApiConfigItemViewModel viewModel)
    {
        if (viewModel == null) return;
        
        var config = viewModel.GetConfig();
        var builtInConfigs = _configurationService.GetBuiltInConfigs();
        
        // Check if config is a built-in config
        var matchingBuiltIn = builtInConfigs.FirstOrDefault(c => c.Id == config.Id);
        if (matchingBuiltIn != null)
        {
            // Save the built-in config
            Storage.DefaultConfig = matchingBuiltIn;
        }
        else
        {
            // Otherwise, save the user config
            Storage.DefaultConfig = config;
        }
        
        _configService.SaveConfig<ApiConfigStorage>();
        OnPropertyChanged(nameof(DefaultConfig));
        LoadDefaultConfigs();
        StatusMessage = $"已设置默认配置: {config.ModelId}";
    }

    /// <summary>
    /// Handle default config selection change
    /// </summary>
    partial void OnSelectedDefaultConfigChanged(ModelApiConfig? value)
    {
        if (value != null)
        {
            var builtInConfigs = _configurationService.GetBuiltInConfigs();
            
            // Check if selected config is a built-in config
            var matchingBuiltIn = builtInConfigs.FirstOrDefault(c => c.Id == value.Id);
            if (matchingBuiltIn != null)
            {
                // Save the built-in config
                Storage.DefaultConfig = matchingBuiltIn;
            }
            else
            {
                // Otherwise, save the selected user config
                Storage.DefaultConfig = value;
            }
            
            _configService.SaveConfig<ApiConfigStorage>();
            OnPropertyChanged(nameof(DefaultConfig));
            StatusMessage = $"已设置默认配置: {value.ModelId}";
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
            
            // Create ViewModel and add to collection
            var viewModel = new ModelApiConfigItemViewModel(newConfig, this);
            Configs.Add(viewModel);
            
            // Automatically start editing the newly added item
            StartEditItem(viewModel);
            
            StatusMessage = $"已添加 {template.Name} 配置，请编辑 API Key 后保存";
        }
    }

    /// <summary>
    /// Remove a configuration item (called by item ViewModel)
    /// </summary>
    public void RemoveConfig(ModelApiConfigItemViewModel viewModel)
    {
        if (viewModel == null) return;
        
        var config = viewModel.GetConfig();
        
        // Show confirmation dialog
        var result = MessageBox.Show(
            $"确定要删除配置 \"{config.ModelId}\" 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes)
        {
            return;
        }
        
        // If removing the editing config, clear editing state
        if (EditingConfig == viewModel)
        {
            EditingConfig = null;
        }
        
        // Clear default config if it's the one being removed
        if (Storage.DefaultConfig != null && Storage.DefaultConfig.Equals(config))
        {
            Storage.DefaultConfig = null;
        }
        
        // Remove from collection
        Configs.Remove(viewModel);
        
        // Save to storage
        SaveConfigs();
        
        // Clear default config if it's no longer in the list
        if (Storage.DefaultConfig != null && !Configs.Any(vm => vm.GetConfig().Equals(Storage.DefaultConfig)))
        {
            Storage.DefaultConfig = null;
            _configService.SaveConfig<ApiConfigStorage>();
        }
        
        UpdateStatusMessage();
        LoadDefaultConfigs();
    }

    /// <summary>
    /// Start editing a configuration item (called by item ViewModel)
    /// </summary>
    public void StartEditItem(ModelApiConfigItemViewModel viewModel)
    {
        // Don't allow editing read-only configs
        if (viewModel.IsReadOnly)
        {
            StatusMessage = "只读配置无法编辑";
            return;
        }
        
        if (viewModel == null) return;
        
        // Cancel previous editing if any
        if (EditingConfig != null && EditingConfig != viewModel)
        {
            EditingConfig.CancelEdit();
        }
        
        // Start editing this ViewModel
        viewModel.BeginEdit();
        EditingConfig = viewModel;
        
        StatusMessage = "正在编辑配置";
    }

    /// <summary>
    /// Save current editing item (called by item ViewModel)
    /// </summary>
    public void SaveEditItem(ModelApiConfigItemViewModel viewModel)
    {
        if (viewModel == null || EditingConfig != viewModel) return;
        
        // Save changes in ViewModel (updates underlying config)
        viewModel.SaveChanges();
        
        // Clear editing state
        EditingConfig = null;
        
        // Save to storage
        SaveConfigs();
        
        // Clear default config if it's no longer in the list
        if (Storage.DefaultConfig != null && !Configs.Any(vm => vm.GetConfig().Equals(Storage.DefaultConfig)))
        {
            Storage.DefaultConfig = null;
            _configService.SaveConfig<ApiConfigStorage>();
        }
        
        UpdateStatusMessage();
        LoadDefaultConfigs();
        StatusMessage = "配置已保存";
    }

    /// <summary>
    /// Cancel editing item (called by item ViewModel)
    /// </summary>
    public void CancelEditItem(ModelApiConfigItemViewModel viewModel)
    {
        if (viewModel == null || EditingConfig != viewModel) return;
        
        // Cancel changes in ViewModel (restores original values)
        viewModel.CancelEdit();
        
        // Clear editing state
        EditingConfig = null;
        
        StatusMessage = "已取消编辑";
    }

    /// <summary>
    /// Save all configurations to storage
    /// </summary>
    [RelayCommand]
    private void SaveAll()
    {
        try
        {
            // Save all configs from ViewModels to Storage
            SaveConfigs();
            
            // Clear default config if it's no longer in the list
            if (Storage.DefaultConfig != null && !Configs.Any(vm => vm.GetConfig().Equals(Storage.DefaultConfig)))
            {
                Storage.DefaultConfig = null;
                _configService.SaveConfig<ApiConfigStorage>();
            }
            
            LoadDefaultConfigs();
            StatusMessage = $"已保存 {Configs.Count} 个配置";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

