using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using BatchRenameTool.State;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace BatchRenameTool.Services
{
    /// <summary>
    /// Configuration service using Quicker's state storage
    /// Supports GetConfig&lt;T&gt;() and auto-save on property changes
    /// </summary>
    public class ConfigService : IDisposable
    {
        private readonly GlobalStateWriter _stateWriter;
        private readonly ConcurrentDictionary<Type, object> _configDict = new();
        private readonly ConcurrentDictionary<Type, PropertyChangedEventHandler> _handlers = new();

        public ConfigService()
        {
            // Use the full type name as the state ID
            _stateWriter = new GlobalStateWriter(typeof(ConfigService).FullName ?? "BatchRenameTool.ConfigService");
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

            // Load from Quicker state storage
            var key = typeof(T).Name;
            var data = _stateWriter.Read(key) as string;
            
            T cfg;
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    cfg = JsonConvert.DeserializeObject<T>(data) ?? new T();
                }
                catch
                {
                    cfg = new T();
                }
            }
            else
            {
                cfg = new T();
            }

            _configDict[typeof(T)] = cfg;

            // Subscribe to property changes for auto-save
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(cfg);
                    _stateWriter.Write(key, json);
                }
                catch
                {
                    // Ignore save errors
                }
            };

            cfg.PropertyChanged += handler;
            _handlers[typeof(T)] = handler;

            return cfg;
        }

        /// <summary>
        /// Manually save configuration
        /// </summary>
        public void SaveConfig<T>() where T : ObservableObject
        {
            if (!_configDict.TryGetValue(typeof(T), out var config))
            {
                return;
            }

            try
            {
                var key = typeof(T).Name;
                var json = JsonConvert.SerializeObject((T)config);
                _stateWriter.Write(key, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public void Dispose()
        {
            // Unsubscribe from all property change events
            foreach (var kvp in _handlers)
            {
                if (_configDict.TryGetValue(kvp.Key, out var config) && config is INotifyPropertyChanged notifyConfig)
                {
                    notifyConfig.PropertyChanged -= kvp.Value;
                }
            }
            
            _handlers.Clear();
            _configDict.Clear();
        }
    }
}
