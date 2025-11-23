using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Linq;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Configuration service for unified access to global and user configurations
/// Ensures singleton pattern for each config type and auto-saves on changes
/// </summary>
public class ConfigService : IDisposable
{
    private readonly ILogger<ConfigService>? _logger;
    private readonly ConcurrentDictionary<Type, object> _configDict = new();
    private readonly ConcurrentDictionary<Type, IDisposable> _subscriptions = new();

    public string ConfigsFolder { get; }

    public ConfigService(ILogger<ConfigService>? logger = null)
    {
        _logger = logger;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "QuickerExpressionAgent");
        Directory.CreateDirectory(appFolder);
        ConfigsFolder = Path.Combine(appFolder, "Configs");
        Directory.CreateDirectory(ConfigsFolder);
    }

    /// <summary>
    /// Get configuration of specified type (singleton pattern)
    /// </summary>
    /// <typeparam name="T">Configuration type (must inherit ObservableObject)</typeparam>
    /// <returns>Configuration instance</returns>
    public T GetConfig<T>() where T : ObservableObject, new()
    {
        if (_configDict.TryGetValue(typeof(T), out var config))
        {
            return (T)config;
        }

        var savePath = Path.Combine(ConfigsFolder, $"{typeof(T).Name}.json");
        
        // Load from file or create new instance
        T cfg;
        if (File.Exists(savePath))
        {
            var json = File.ReadAllText(savePath);
            cfg = json.FromJson<T>() ?? new T();
        }
        else
        {
            // Try to import from old location (backward compatibility)
            cfg = TryImportFromOldLocation<T>() ?? new T();
        }

        _configDict[typeof(T)] = cfg;

        // Subscribe to property changes and auto-save
        var subscription = Observable.FromEventPattern<System.ComponentModel.PropertyChangedEventHandler, System.ComponentModel.PropertyChangedEventArgs>(
                h => cfg.PropertyChanged += h,
                h => cfg.PropertyChanged -= h)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOnDispatcher()
            .Subscribe(evt =>
            {
                _logger?.LogInformation("Config Changed, Type: {Type}, Property: {PropertyName}", typeof(T).Name, evt.EventArgs.PropertyName);
                var json = cfg.ToJson(indented: true);
                File.WriteAllText(savePath, json);
            });

        _subscriptions[typeof(T)] = subscription;

        return cfg;
    }

    /// <summary>
    /// Manually save configuration to file
    /// </summary>
    public void SaveConfig<T>() where T : ObservableObject
    {
        if (!_configDict.TryGetValue(typeof(T), out var config))
        {
            return;
        }

        var savePath = Path.Combine(ConfigsFolder, $"{typeof(T).Name}.json");
        try
        {
            var json = ((T)config).ToJson(indented: true);
            File.WriteAllText(savePath, json);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save config {Type} to {Path}", typeof(T).Name, savePath);
        }
    }

    /// <summary>
    /// Try to import configuration from old location (backward compatibility)
    /// This method can be deleted after initial release
    /// </summary>
    private T? TryImportFromOldLocation<T>() where T : ObservableObject, new()
    {
        // Only handle ApiConfigStorage for backward compatibility
        if (typeof(T) != typeof(ApiConfigStorage))
        {
            return null;
        }

        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "QuickerExpressionAgent");
            var oldConfigPath = Path.Combine(appFolder, "api-configs.json");
            
            if (!File.Exists(oldConfigPath))
            {
                return null;
            }

            var json = File.ReadAllText(oldConfigPath);
            
            // Try to deserialize as ApiConfigStorage (new format)
            var storage = json.FromJson<ApiConfigStorage>();
            if (storage != null)
            {
                // Migrate to new location
                var newPath = Path.Combine(ConfigsFolder, "ApiConfigStorage.json");
                File.WriteAllText(newPath, json);
                _logger?.LogInformation("Migrated ApiConfigStorage from old location to {NewPath}", newPath);
                return (T)(object)storage;
            }
            
            // Backward compatibility: try to deserialize as List<ModelApiConfig> (old format)
            var configs = json.FromJson<List<QuickerExpressionAgent.Server.Services.ModelApiConfig>>();
            if (configs != null)
            {
                var storage2 = new ApiConfigStorage 
                { 
                    Configs = new(configs)
                };
                // Save to new location
                var newPath = Path.Combine(ConfigsFolder, "ApiConfigStorage.json");
                var newJson = storage2.ToJson(indented: true);
                File.WriteAllText(newPath, newJson);
                _logger?.LogInformation("Migrated ApiConfigStorage from old format to new location {NewPath}", newPath);
                return (T)(object)storage2;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to import {Type} from old location", typeof(T).Name);
        }
        
        return null;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        _configDict.Clear();
    }
}

