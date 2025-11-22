# DPAPI 加密存储 API Key 实现示例

## 概述

本文档提供了使用 Windows DPAPI（Data Protection API）安全存储 API Key 的完整实现示例。

## 实现代码

### 1. SecureStorage 服务类

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Secure storage service using Windows DPAPI
/// </summary>
public class SecureStorageService
{
    private const string RegistryKeyPath = @"SOFTWARE\QuickerExpressionAgent";
    private const string ConfigValueName = "EncryptedConfig";
    private readonly string _configFilePath;

    public SecureStorageService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "QuickerExpressionAgent");
        Directory.CreateDirectory(appFolder);
        _configFilePath = Path.Combine(appFolder, "config.encrypted");
    }

    /// <summary>
    /// Encrypt and save API configuration
    /// </summary>
    public void SaveEncryptedConfig(string apiKey, string baseUrl, string modelId)
    {
        var config = new
        {
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            ModelId = modelId,
            EncryptedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(config);
        var encrypted = Encrypt(json);
        
        // Save to file
        File.WriteAllText(_configFilePath, encrypted);
        
        // Also save to registry (optional, as backup)
        SaveToRegistry(encrypted);
    }

    /// <summary>
    /// Load and decrypt API configuration
    /// </summary>
    public (string ApiKey, string BaseUrl, string ModelId)? LoadEncryptedConfig()
    {
        string? encrypted = null;

        // Try to load from file first
        if (File.Exists(_configFilePath))
        {
            encrypted = File.ReadAllText(_configFilePath);
        }
        // Fallback to registry
        else
        {
            encrypted = LoadFromRegistry();
        }

        if (string.IsNullOrEmpty(encrypted))
        {
            return null;
        }

        try
        {
            var decrypted = Decrypt(encrypted);
            var config = JsonSerializer.Deserialize<ConfigModel>(decrypted);
            
            if (config != null)
            {
                return (config.ApiKey, config.BaseUrl, config.ModelId);
            }
        }
        catch (CryptographicException)
        {
            // Decryption failed - possibly different user or machine
            return null;
        }

        return null;
    }

    /// <summary>
    /// Check if encrypted config exists
    /// </summary>
    public bool HasEncryptedConfig()
    {
        return File.Exists(_configFilePath) || LoadFromRegistry() != null;
    }

    /// <summary>
    /// Delete encrypted config
    /// </summary>
    public void DeleteEncryptedConfig()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
        DeleteFromRegistry();
    }

    /// <summary>
    /// Encrypt data using DPAPI (CurrentUser scope)
    /// </summary>
    private string Encrypt(string plainText)
    {
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        
        // Use CurrentUser scope - only current user can decrypt
        // Use LocalMachine scope if you want machine-wide encryption
        byte[] encrypted = ProtectedData.Protect(
            data,
            null, // Optional entropy (additional security)
            DataProtectionScope.CurrentUser
        );

        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypt data using DPAPI
    /// </summary>
    private string Decrypt(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        
        byte[] decrypted = ProtectedData.Unprotect(
            data,
            null,
            DataProtectionScope.CurrentUser
        );

        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Save encrypted data to registry (optional backup)
    /// </summary>
    private void SaveToRegistry(string encrypted)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(ConfigValueName, encrypted, RegistryValueKind.String);
        }
        catch
        {
            // Ignore registry errors
        }
    }

    /// <summary>
    /// Load encrypted data from registry
    /// </summary>
    private string? LoadFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            return key?.GetValue(ConfigValueName) as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete encrypted data from registry
    /// </summary>
    private void DeleteFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(ConfigValueName, false);
        }
        catch
        {
            // Ignore errors
        }
    }

    private class ConfigModel
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public DateTime EncryptedAt { get; set; }
    }
}
```

### 2. 更新 ConfigurationService

```csharp
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using QuickerExpressionAgent.Server.Generated;

namespace QuickerExpressionAgent.Server.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly SecureStorageService _secureStorage;
    public IConfiguration Configuration { get; }

    public ConfigurationService()
    {
        _secureStorage = new SecureStorageService();
        var configBuilder = new ConfigurationBuilder();
        
        // Priority 1: Load from encrypted storage (user-configured)
        var encryptedConfig = _secureStorage.LoadEncryptedConfig();
        if (encryptedConfig.HasValue)
        {
            var config = encryptedConfig.Value;
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = config.ApiKey,
                ["OpenAI:BaseUrl"] = config.BaseUrl,
                ["OpenAI:ModelId"] = config.ModelId
            };
            configBuilder.AddInMemoryCollection(inMemoryConfig);
        }
        // Priority 2: Embedded config (default, first run)
        else
        {
            var embeddedApiKey = EmbeddedConfig.ApiKey;
            var embeddedBaseUrl = EmbeddedConfig.BaseUrl;
            var embeddedModelId = EmbeddedConfig.ModelId;
            
            if (!string.IsNullOrEmpty(embeddedApiKey))
            {
                var embeddedConfig = new Dictionary<string, string?>
                {
                    ["OpenAI:ApiKey"] = embeddedApiKey,
                    ["OpenAI:BaseUrl"] = embeddedBaseUrl,
                    ["OpenAI:ModelId"] = embeddedModelId
                };
                configBuilder.AddInMemoryCollection(embeddedConfig);
            }
        }
        
        // Priority 3: File system (for development)
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? Directory.GetCurrentDirectory();
        
        var configPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
            Path.Combine(basePath, "appsettings.json")
        };

        foreach (var path in configPaths)
        {
            if (File.Exists(path))
            {
                configBuilder.AddJsonFile(path, optional: true, reloadOnChange: false);
            }
        }

        Configuration = configBuilder
            .AddEnvironmentVariables()
            .Build();
    }

    public string GetApiKey()
    {
        return Configuration["OpenAI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? "";
    }

    public string GetBaseUrl()
    {
        return Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
    }

    public string GetModelId()
    {
        return Configuration["OpenAI:ModelId"] ?? "deepseek-chat";
    }

    /// <summary>
    /// Save API configuration securely
    /// </summary>
    public void SaveApiConfiguration(string apiKey, string baseUrl, string modelId)
    {
        _secureStorage.SaveEncryptedConfig(apiKey, baseUrl, modelId);
    }

    /// <summary>
    /// Check if user has configured API key
    /// </summary>
    public bool HasUserConfiguration()
    {
        return _secureStorage.HasEncryptedConfig();
    }
}
```

### 3. 配置界面示例（ViewModel）

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickerExpressionAgent.Server.Services;

namespace QuickerExpressionAgent.Server.ViewModels;

public partial class ApiConfigViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;

    [ObservableProperty]
    public partial string ApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BaseUrl { get; set; } = "https://api.deepseek.com/v1";

    [ObservableProperty]
    public partial string ModelId { get; set; } = "deepseek-chat";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaved { get; set; }

    public ApiConfigViewModel(ConfigurationService configService)
    {
        _configService = configService;
        
        // Load existing configuration if available
        if (_configService.HasUserConfiguration())
        {
            ApiKey = _configService.GetApiKey();
            BaseUrl = _configService.GetBaseUrl();
            ModelId = _configService.GetModelId();
            IsSaved = true;
            StatusMessage = "已加载保存的配置";
        }
    }

    [RelayCommand]
    private void SaveConfiguration()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "错误：API Key 不能为空";
            IsSaved = false;
            return;
        }

        try
        {
            _configService.SaveApiConfiguration(ApiKey, BaseUrl, ModelId);
            IsSaved = true;
            StatusMessage = "配置已安全保存";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败：{ex.Message}";
            IsSaved = false;
        }
    }

    [RelayCommand]
    private void ClearConfiguration()
    {
        ApiKey = string.Empty;
        BaseUrl = "https://api.deepseek.com/v1";
        ModelId = "deepseek-chat";
        IsSaved = false;
        StatusMessage = "配置已清除";
    }
}
```

## 使用说明

### 1. 添加必要的 using

```csharp
using System.Security.Cryptography; // For ProtectedData
using Microsoft.Win32; // For Registry (optional)
```

### 2. 数据保护范围选择

**CurrentUser**（推荐）：
- 只有当前登录用户可以解密
- 其他用户无法访问
- 适合单用户应用

```csharp
DataProtectionScope.CurrentUser
```

**LocalMachine**：
- 同一台机器上的所有用户都可以解密
- 适合多用户共享的应用
- 安全性较低

```csharp
DataProtectionScope.LocalMachine
```

### 3. 可选：添加熵（Entropy）增加安全性

```csharp
// Generate a random entropy (store it securely)
private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("YourAppSpecificEntropy");

// Use in encryption
byte[] encrypted = ProtectedData.Protect(
    data,
    Entropy, // Additional security
    DataProtectionScope.CurrentUser
);

// Use in decryption
byte[] decrypted = ProtectedData.Unprotect(
    data,
    Entropy,
    DataProtectionScope.CurrentUser
);
```

### 4. 错误处理

DPAPI 可能失败的情况：
- 用户切换（CurrentUser scope）
- 系统重装
- 权限问题

应该提供降级方案：
```csharp
try
{
    var config = _secureStorage.LoadEncryptedConfig();
}
catch (CryptographicException)
{
    // Fallback to embedded config or prompt user
    return LoadEmbeddedConfig();
}
```

## 安全注意事项

1. **DPAPI 不是万能的**：
   - 仍然可以通过调试器在运行时提取
   - 需要配合代码混淆和反调试措施

2. **存储位置**：
   - 文件：`%AppData%\QuickerExpressionAgent\config.encrypted`
   - 注册表：`HKCU\SOFTWARE\QuickerExpressionAgent`

3. **备份策略**：
   - 可以同时存储到文件和注册表
   - 提供导出/导入功能（加密格式）

4. **清理**：
   - 卸载时清理加密数据
   - 提供"清除配置"功能

## 测试

```csharp
// Test encryption/decryption
var service = new SecureStorageService();
service.SaveEncryptedConfig("test-key", "https://api.test.com", "test-model");
var loaded = service.LoadEncryptedConfig();
Console.WriteLine($"ApiKey: {loaded?.ApiKey}"); // Should output: test-key
```

## 总结

使用 DPAPI 的优势：
- ✅ Windows 原生支持
- ✅ 实现相对简单
- ✅ 基于用户/机器的加密
- ✅ 无需管理加密密钥

配合其他措施：
- 代码混淆
- 反调试保护
- API Key 使用限制和监控

