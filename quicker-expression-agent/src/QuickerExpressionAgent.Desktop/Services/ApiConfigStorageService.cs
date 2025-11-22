using QuickerExpressionAgent.Common;
using QuickerExpressionAgent.Server.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickerExpressionAgent.Desktop.Services;

/// <summary>
/// Service for storing and loading ModelApiConfig list
/// </summary>
public class ApiConfigStorageService
{
    private readonly string _configFilePath;

    public ApiConfigStorageService()
    {
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "QuickerExpressionAgent");
        Directory.CreateDirectory(appFolder);
        _configFilePath = Path.Combine(appFolder, "api-configs.json");
    }

    /// <summary>
    /// Save configurations to file
    /// </summary>
    public void SaveConfigs(IEnumerable<ModelApiConfig> configs)
    {
        var configList = configs.ToList();
        var json = configList.ToJson(indented: true);
        File.WriteAllText(_configFilePath, json);
    }

    /// <summary>
    /// Load configurations from file
    /// </summary>
    public List<ModelApiConfig> LoadConfigs()
    {
        if (!File.Exists(_configFilePath))
        {
            return new List<ModelApiConfig>();
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var configs = json.FromJson<List<ModelApiConfig>>();
            return configs ?? new List<ModelApiConfig>();
        }
        catch
        {
            return new List<ModelApiConfig>();
        }
    }
}

