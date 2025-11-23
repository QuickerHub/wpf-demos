using CommunityToolkit.Mvvm.ComponentModel;
using QuickerExpressionAgent.Server.Services;
using System;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for managing the current active API configuration
/// </summary>
public class CurrentApiConfigService : ObservableObject
{
    private ModelApiConfig? _currentConfig;
    private readonly ApiConfigStorageService _storageService;

    /// <summary>
    /// Current active API configuration
    /// </summary>
    public ModelApiConfig? CurrentConfig
    {
        get => _currentConfig;
        private set
        {
            if (SetProperty(ref _currentConfig, value))
            {
                // Notify when config changes
                CurrentConfigChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Event raised when current config changes
    /// </summary>
    public event EventHandler? CurrentConfigChanged;

    public CurrentApiConfigService(ApiConfigStorageService storageService)
    {
        _storageService = storageService;
        LoadDefaultConfig();
    }

    /// <summary>
    /// Load default config (first available config or create default)
    /// </summary>
    private void LoadDefaultConfig()
    {
        var configs = _storageService.LoadConfigs();
        if (configs.Count > 0)
        {
            // Use first config as default
            CurrentConfig = configs[0];
        }
        else
        {
            // Create default config
            CurrentConfig = new ModelApiConfig
            {
                ApiKey = string.Empty,
                ModelId = "deepseek-chat",
                BaseUrl = "https://api.deepseek.com/v1"
            };
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

        // Create a new instance to ensure SetProperty detects the change (reference comparison)
        // This allows retry after failure even if values are the same
        var newConfig = new ModelApiConfig
        {
            ApiKey = config.ApiKey,
            ModelId = config.ModelId,
            BaseUrl = config.BaseUrl
        };
        
        CurrentConfig = newConfig;
    }

    /// <summary>
    /// Reload configs from storage and update current config if needed
    /// </summary>
    public void ReloadConfigs()
    {
        var configs = _storageService.LoadConfigs();
        
        // If current config still exists, keep it; otherwise use first available
        if (CurrentConfig != null && configs.Any(c => 
            c.ApiKey == CurrentConfig.ApiKey && 
            c.ModelId == CurrentConfig.ModelId && 
            c.BaseUrl == CurrentConfig.BaseUrl))
        {
            // Current config still exists, update it with latest values
            var updated = configs.First(c => 
                c.ApiKey == CurrentConfig.ApiKey && 
                c.ModelId == CurrentConfig.ModelId && 
                c.BaseUrl == CurrentConfig.BaseUrl);
            // Only update if values changed to avoid unnecessary events
            // SetProperty will only trigger event if the value actually changed
            if (updated.ApiKey != CurrentConfig.ApiKey || 
                updated.ModelId != CurrentConfig.ModelId || 
                updated.BaseUrl != CurrentConfig.BaseUrl)
            {
                CurrentConfig = updated;
            }
        }
        else if (configs.Count > 0)
        {
            // Current config no longer exists, switch to first available
            // Only switch if it's actually different
            var firstConfig = configs[0];
            if (CurrentConfig == null || 
                firstConfig.ApiKey != CurrentConfig.ApiKey || 
                firstConfig.ModelId != CurrentConfig.ModelId || 
                firstConfig.BaseUrl != CurrentConfig.BaseUrl)
            {
                CurrentConfig = firstConfig;
            }
        }
        else
        {
            // No configs available, create default
            // Only create default if current config is not already a default
            if (CurrentConfig == null || 
                !string.IsNullOrEmpty(CurrentConfig.ApiKey) ||
                CurrentConfig.ModelId != "deepseek-chat" ||
                CurrentConfig.BaseUrl != "https://api.deepseek.com/v1")
            {
                LoadDefaultConfig();
            }
        }
    }
}

