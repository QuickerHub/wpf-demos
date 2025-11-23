using CommunityToolkit.Mvvm.ComponentModel;
using QuickerExpressionAgent.Server.Services;
using System.Collections.Generic;
using System.Linq;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Storage model for API configurations and default config
/// Inherits ObservableObject for automatic change tracking and saving via ConfigService
/// </summary>
public partial class ApiConfigStorage : ObservableObject
{
    /// <summary>
    /// List of user-configured API configurations (editable)
    /// </summary>
    [ObservableProperty]
    private List<ModelApiConfig> _configs = new();

    /// <summary>
    /// Default API configuration (selected from Configs)
    /// </summary>
    [ObservableProperty]
    private ModelApiConfig? _defaultConfig;

    /// <summary>
    /// Get all available configurations including built-in configs and user configs
    /// </summary>
    /// <param name="builtInConfigs">Built-in configs from IConfigurationService (read-only)</param>
    /// <returns>List of available configs with built-in configs first, then user configs</returns>
    public List<ModelApiConfig> GetAvailableConfigs(IReadOnlyList<ModelApiConfig> builtInConfigs)
    {
        var availableConfigs = new List<ModelApiConfig>();
        
        // Add built-in configs first (read-only)
        foreach (var builtInConfig in builtInConfigs)
        {
            // Clone to avoid modifying the original
            var displayConfig = builtInConfig.Clone();
            displayConfig.IsReadOnly = true; // Ensure read-only flag is set
            availableConfigs.Add(displayConfig);
        }
        
        // Add user-configured configs (editable)
        availableConfigs.AddRange(Configs);
        
        return availableConfigs;
    }

    /// <summary>
    /// Get the config that should be selected based on saved default config and current config
    /// </summary>
    /// <param name="availableConfigs">List of available configs (from GetAvailableConfigs)</param>
    /// <param name="defaultConfig">Default config from IConfigurationService</param>
    /// <param name="currentConfig">Current active config</param>
    /// <returns>Config that should be selected, or first available if none matches</returns>
    public ModelApiConfig? GetSelectedConfig(
        List<ModelApiConfig> availableConfigs,
        ModelApiConfig? defaultConfig,
        ModelApiConfig? currentConfig)
    {
        // Priority 1: Use saved default config if it exists and is valid
        if (DefaultConfig != null && !string.IsNullOrWhiteSpace(DefaultConfig.ApiKey))
        {
            var matchingSavedDefault = availableConfigs.FirstOrDefault(c => c.Id == DefaultConfig.Id);
            if (matchingSavedDefault != null)
            {
                return matchingSavedDefault;
            }
        }

        // Priority 2: Use current config if it matches default config
        if (currentConfig != null && defaultConfig != null && currentConfig.Id == defaultConfig.Id)
        {
            return availableConfigs.FirstOrDefault();
        }

        // Priority 3: Try to match current config in available configs
        if (currentConfig != null)
        {
            var matchingCurrent = availableConfigs.FirstOrDefault(c => c.Id == currentConfig.Id);
            if (matchingCurrent != null)
            {
                return matchingCurrent;
            }
        }

        // Priority 4: Use first available config
        return availableConfigs.FirstOrDefault();
    }

    /// <summary>
    /// Get the actual config to use (not the display config) based on selected config
    /// </summary>
    /// <param name="selectedConfig">Selected config from UI</param>
    /// <param name="defaultConfig">Default config from IConfigurationService</param>
    /// <returns>Actual config to use for agent creation</returns>
    public ModelApiConfig? GetActualConfig(ModelApiConfig? selectedConfig, ModelApiConfig? defaultConfig)
    {
        // If selected config is the default display config, return the actual default config
        if (selectedConfig != null && defaultConfig != null && 
            selectedConfig.Id == defaultConfig.Id && 
            selectedConfig.Title == "default")
        {
            return defaultConfig;
        }

        return selectedConfig;
    }
}

