using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Configuration service for managing application settings
/// </summary>
public class ConfigService
{
    public string ConfigFolder { get; }

    public ConfigService()
    {
        ConfigFolder = Path.Combine(BaseFolder, "QuickerExpressionAgent.Quicker");
        Directory.CreateDirectory(ConfigFolder);
        
        var path = Path.Combine(ConfigFolder, "startup.json");
        StartupConfig = LoadConfig<StartupConfig>(path);
        StartupConfig.PropertyChanged += (s, e) => SaveConfig(StartupConfig, path);
    }

    public StartupConfig StartupConfig { get; }

    public static string AssemblyPath { get; } = Assembly.GetExecutingAssembly().Location;
    
    public static string BaseFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
        ?? throw new InvalidOperationException("Unable to determine assembly directory");

    /// <summary>
    /// Load configuration from file, or create default if file doesn't exist
    /// </summary>
    private static T LoadConfig<T>(string filePath) where T : ObservableObject, new()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new T();
            }

            var fileContent = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<T>(fileContent);
            return config ?? new T();
        }
        catch (Exception ex)
        {
            // Log error and return default config
            Console.WriteLine($"Error loading config from {filePath}: {ex.Message}");
            return new T();
        }
    }

    /// <summary>
    /// Save configuration to file
    /// </summary>
    private static void SaveConfig<T>(T config, string path) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config to {path}: {ex.Message}");
        }
    }
}

