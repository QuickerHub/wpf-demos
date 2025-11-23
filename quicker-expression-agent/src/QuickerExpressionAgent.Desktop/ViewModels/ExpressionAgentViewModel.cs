using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Server.Agent;
using QuickerExpressionAgent.Server.Services;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly ApiConfigListViewModel _apiConfigListViewModel;
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
    /// Available API configurations (bound to ApiConfigListViewModel.AvailableApiConfigs)
    /// </summary>
    public ObservableCollection<ModelApiConfig> AvailableApiConfigs { get; }

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
        ApiConfigListViewModel apiConfigListViewModel,
        ExpressionExecutor executor,
        ILogger<ExpressionAgentViewModel>? logger = null)
    {
        _configurationService = configurationService;
        _apiConfigListViewModel = apiConfigListViewModel;
        _executor = executor;
        _logger = logger;

        // Bind to AvailableApiConfigs from ApiConfigListViewModel (automatically updates)
        AvailableApiConfigs = _apiConfigListViewModel.AvailableApiConfigs;

        // Get initial default config (user-set default or embedded default)
        CurrentConfig = _apiConfigListViewModel.DefaultConfig;

        // Set selected config to match current config
        if (CurrentConfig != null && AvailableApiConfigs.Count > 0)
        {
            // Find matching config by Id
            var matchingConfig = AvailableApiConfigs.FirstOrDefault(c => c.Id == CurrentConfig.Id);
            
            if (matchingConfig != null)
            {
                SelectedApiConfig = matchingConfig;
            }
            else
            {
                // Fallback to first available config
                SelectedApiConfig = AvailableApiConfigs.FirstOrDefault();
            }
        }

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
                    var embeddedDefaultConfig = _configurationService.GetConfig();
                    var previousConfig = AvailableApiConfigs.FirstOrDefault(c => 
                        (embeddedDefaultConfig != null && c.Equals(embeddedDefaultConfig) && c.Title == "default" && CurrentConfig.Equals(embeddedDefaultConfig)) ||
                        (!string.IsNullOrEmpty(c.Title) && c.Title != "default" && c.Equals(CurrentConfig)));
                    
                    if (previousConfig != null)
                    {
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
            
            // Convert display config to actual config
            var actualConfig = _apiConfigListViewModel.GetActualConfig(value);
            
            if (actualConfig != null && actualConfig.Id != CurrentConfig?.Id)
            {
                SwitchConfig(actualConfig);
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
            // Get config (use default if CurrentConfig is null or has empty API key)
            var config = CurrentConfig;
            if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                config = _apiConfigListViewModel.DefaultConfig;
                CurrentConfig = config;

                _logger?.LogWarning("No valid config available, cannot create agent");
                _agent = null;
                OnPropertyChanged(nameof(Agent));
                StatusText = "API 配置未设置，请先配置 API Key";
                AgentRecreated?.Invoke(this, null);
                return;
            }

            _logger?.LogInformation("Recreating agent with config - ModelId: {ModelId}, BaseUrl: {BaseUrl}",
                config.ModelId, config.BaseUrl);

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
            _logger?.LogError(ex, "Error recreating agent");

            // If creation fails, clear agent
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

}

