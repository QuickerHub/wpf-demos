using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for managing ExpressionAgent creation and switching
/// </summary>
public partial class ExpressionAgentViewModel : ObservableObject
{
    private ExpressionAgent? _agent;
    private readonly ExpressionExecutor _executor;
    private readonly ILogger<ExpressionAgentViewModel>? _logger;
    private readonly IConfigurationService _configurationService;
    private readonly ApiConfigStorageService _apiConfigStorageService;
    private IExpressionAgentToolHandler? _toolHandler;

    /// <summary>
    /// Current agent instance (null if not initialized)
    /// </summary>
    public ExpressionAgent? Agent => _agent;

    /// <summary>
    /// Current API configuration
    /// </summary>
    [ObservableProperty]
    private ModelApiConfig? _currentConfig;

    /// <summary>
    /// Available API configurations
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ModelApiConfig> _availableApiConfigs = new();

    /// <summary>
    /// Selected API configuration
    /// </summary>
    [ObservableProperty]
    private ModelApiConfig? _selectedApiConfig;

    /// <summary>
    /// Status message for agent operations
    /// </summary>
    [ObservableProperty]
    private string _statusText = "正在初始化...";

    /// <summary>
    /// Whether the agent is currently being recreated
    /// </summary>
    [ObservableProperty]
    private bool _isRecreating = false;

    /// <summary>
    /// Event raised when agent is recreated
    /// </summary>
    public event EventHandler<ExpressionAgent?>? AgentRecreated;

    public ExpressionAgentViewModel(
        IConfigurationService configurationService,
        ApiConfigStorageService apiConfigStorageService,
        ExpressionExecutor executor,
        ILogger<ExpressionAgentViewModel>? logger = null)
    {
        _configurationService = configurationService;
        _apiConfigStorageService = apiConfigStorageService;
        _executor = executor;
        _logger = logger;

        // Load available configs
        LoadAvailableConfigs();

        // Create initial agent with default config
        RecreateAgent();
    }

    /// <summary>
    /// Set the tool handler for the agent
    /// </summary>
    public void SetToolHandler(IExpressionAgentToolHandler? toolHandler)
    {
        _toolHandler = toolHandler;
        if (_agent != null)
        {
            _agent.ToolHandler = toolHandler;
        }
    }

    /// <summary>
    /// Load available API configurations for model selection dropdown
    /// Note: Includes default config for selection, but default config is NOT saved to storage
    /// </summary>
    private void LoadAvailableConfigs()
    {
        var configs = _apiConfigStorageService.LoadConfigs();
        var defaultConfig = _configurationService.GetConfig();
        
        AvailableApiConfigs.Clear();
        
        // Add default config as first item (if exists)
        if (defaultConfig != null)
        {
            AvailableApiConfigs.Add(new ModelApiConfig
            {
                ApiKey = defaultConfig.ApiKey,
                ModelId = "Default",
                BaseUrl = defaultConfig.BaseUrl
            });
        }
        
        // Add user-configured configs
        foreach (var config in configs)
        {
            AvailableApiConfigs.Add(config);
        }
        
        // Select matching config or first available
        SelectedApiConfig = CurrentConfig != null
            ? AvailableApiConfigs.FirstOrDefault(c =>
                c.ApiKey == CurrentConfig.ApiKey &&
                c.ModelId == CurrentConfig.ModelId &&
                c.BaseUrl == CurrentConfig.BaseUrl)
            ?? AvailableApiConfigs.FirstOrDefault()
            : AvailableApiConfigs.FirstOrDefault();
    }

    /// <summary>
    /// Switch to a different API configuration
    /// </summary>
    public void SwitchConfig(ModelApiConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("API Key cannot be empty. Please configure the API key first.");
        }

        CurrentConfig = config;
        SelectedApiConfig = config;
        RecreateAgent();
    }

    /// <summary>
    /// Handle selected API config change
    /// </summary>
    partial void OnSelectedApiConfigChanged(ModelApiConfig? value)
    {
        if (value != null)
        {
            // Check if API Key is empty before switching
            if (string.IsNullOrWhiteSpace(value.ApiKey))
            {
                // Show message box and revert to previous selection
                MessageBox.Show(
                    "API Key 不能为空，请先配置 API Key。",
                    "配置错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                // Revert to previous selection (CurrentConfig or first available)
                if (CurrentConfig != null)
                {
                    // Try to find the previous config in AvailableApiConfigs
                    var previousConfig = AvailableApiConfigs.FirstOrDefault(c =>
                        c.ApiKey == CurrentConfig.ApiKey &&
                        c.ModelId == CurrentConfig.ModelId &&
                        c.BaseUrl == CurrentConfig.BaseUrl);
                    
                    if (previousConfig != null)
                    {
                        // Temporarily disable property change handler to avoid recursion
                        _selectedApiConfig = previousConfig;
                        OnPropertyChanged(nameof(SelectedApiConfig));
                        return;
                    }
                }
                
                // If no previous config found, select first available (default)
                if (AvailableApiConfigs.Count > 0)
                {
                    _selectedApiConfig = AvailableApiConfigs.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedApiConfig));
                }
                return;
            }
            
            // Check if this is the default config (first item in list)
            var defaultConfig = _configurationService.GetConfig();
            if (value == AvailableApiConfigs.FirstOrDefault() && 
                defaultConfig != null &&
                value.ApiKey == defaultConfig.ApiKey &&
                value.BaseUrl == defaultConfig.BaseUrl)
            {
                // Use the actual default config from configuration service
                SwitchConfig(defaultConfig);
            }
            else if (value != CurrentConfig)
            {
                SwitchConfig(value);
            }
        }
    }

    /// <summary>
    /// Recreate agent with current API configuration
    /// </summary>
    public void RecreateAgent()
    {
        if (IsRecreating)
        {
            return; // Prevent concurrent recreation
        }

        IsRecreating = true;

        try
        {
            var config = CurrentConfig;
            var isUsingDefaultConfig = false;

            // Use default config from IConfigurationService if current config is null or has empty API key
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                _logger?.LogInformation("CurrentConfig is null or has empty API key, using default config from IConfigurationService");
                config = _configurationService.GetConfig();
                isUsingDefaultConfig = true;
                CurrentConfig = config;
                
                // Update SelectedApiConfig to match the default config (first item in AvailableApiConfigs)
                if (AvailableApiConfigs.Count > 0)
                {
                    var defaultDisplayConfig = AvailableApiConfigs.FirstOrDefault();
                    if (defaultDisplayConfig != null && 
                        defaultDisplayConfig.ApiKey == config.ApiKey &&
                        defaultDisplayConfig.BaseUrl == config.BaseUrl)
                    {
                        SelectedApiConfig = defaultDisplayConfig;
                    }
                }
            }

            // Must check API key is not empty before creating agent
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                // Even default config has no API key - cannot create agent
                _logger?.LogWarning("Default config also has no API key, cannot create agent. ModelId: {ModelId}, BaseUrl: {BaseUrl}",
                    config.ModelId, config.BaseUrl);
                _agent = null;
                OnPropertyChanged(nameof(Agent));
                StatusText = "API 配置未设置，请先配置 API Key";
                AgentRecreated?.Invoke(this, null);
                return;
            }

            _logger?.LogInformation("Recreating agent with config - ModelId: {ModelId}, BaseUrl: {BaseUrl}, IsDefault: {IsDefault}",
                config.ModelId, config.BaseUrl, isUsingDefaultConfig);

            var kernel = KernelService.GetKernel(config);
            _agent = new ExpressionAgent(kernel, _executor, _toolHandler);

            // Update tool handler if set
            if (_toolHandler != null)
            {
                _agent.ToolHandler = _toolHandler;
            }

            // Notify agent change
            OnPropertyChanged(nameof(Agent));
            StatusText = $"已切换到模型: {config.ModelId}";
            AgentRecreated?.Invoke(this, _agent);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error recreating agent, falling back to default config");

            // On failure, try to use default config
            try
            {
                var defaultConfig = _configurationService.GetConfig();
                if (!string.IsNullOrWhiteSpace(defaultConfig.ApiKey))
                {
                    _logger?.LogInformation("Falling back to default config - ModelId: {ModelId}, BaseUrl: {BaseUrl}",
                        defaultConfig.ModelId, defaultConfig.BaseUrl);

                    var kernel = KernelService.GetKernel(defaultConfig);
                    _agent = new ExpressionAgent(kernel, _executor, _toolHandler);

                    // Update tool handler if set
                    if (_toolHandler != null)
                    {
                        _agent.ToolHandler = _toolHandler;
                    }

                    // Notify agent change
                    OnPropertyChanged(nameof(Agent));
                    StatusText = $"切换模型失败，已切换到默认模型: {defaultConfig.ModelId}";
                    AgentRecreated?.Invoke(this, _agent);
                    return;
                }
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Error falling back to default config");
            }

            // If fallback also fails, clear agent
            _agent = null;
            OnPropertyChanged(nameof(Agent));
            StatusText = $"切换模型失败: {ex.Message}";
            AgentRecreated?.Invoke(this, null);
        }
        finally
        {
            IsRecreating = false;
        }
    }

    /// <summary>
    /// Reload available configurations
    /// </summary>
    public void ReloadConfigs()
    {
        LoadAvailableConfigs();
    }
}

