# AI 启动配置流程

## 当前配置读取逻辑

### 1. ConfigurationService 配置读取顺序

#### GetApiKey()
```csharp
EmbeddedConfig.ApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? ""
```
- **优先级 1**: `EmbeddedConfig.ApiKey`（编译时从 `.env` 文件生成）
- **优先级 2**: 环境变量 `OPENAI_API_KEY`
- **默认值**: 空字符串（如果为空，KernelService 会抛出异常）

#### GetBaseUrl()
```csharp
Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1"
```
- **优先级 1**: 环境变量 `OpenAI:BaseUrl`（注意：环境变量格式是 `OPENAI_BASE_URL`，但 Configuration 会转换为 `OpenAI:BaseUrl`）
- **默认值**: `"https://api.openai.com/v1"`

#### GetModelId()
```csharp
Configuration["OpenAI:ModelId"] ?? "deepseek-chat"
```
- **优先级 1**: 环境变量 `OpenAI:ModelId`（注意：环境变量格式是 `OPENAI_MODEL_ID`，但 Configuration 会转换为 `OpenAI:ModelId`）
- **默认值**: `"deepseek-chat"`

### 2. 配置初始化流程

1. **ChatWindowViewModel 构造函数**:
   ```csharp
   var kernel = KernelService.GetKernel(configurationService);
   _agent = new ExpressionAgent(kernel, executor, _defaultToolHandler);
   ```

2. **KernelService.GetKernel(IConfigurationService)**:
   ```csharp
   var apiKey = configurationService.GetApiKey();
   var baseUrl = configurationService.GetBaseUrl();
   var modelId = configurationService.GetModelId();
   return GetKernel(apiKey, baseUrl, modelId);
   ```

3. **KernelService.GetKernel(string, string, string)**:
   - 验证 API Key 不为空
   - 创建 Kernel 并配置 OpenAI Chat Completion
   - 使用指定的 `baseUrl` 和 `modelId`

## 当前配置状态

### .env 文件内容
```
OPENAI_API_KEY=sk-8d4b453eb4aa4bc9b977d203c0a3ad0c
OPENAI_BASE_URL=https://api.deepseek.com
OPENAI_MODEL_ID=deepseek-chat
```

### 潜在问题

1. **BaseUrl 和 ModelId 无法从 .env 读取**:
   - `EmbeddedConfig` 只包含 `ApiKey`，不包含 `BaseUrl` 和 `ModelId`
   - `GetBaseUrl()` 和 `GetModelId()` 只从环境变量读取，但环境变量格式可能不匹配
   - 环境变量 `OPENAI_BASE_URL` 会被转换为 `OpenAI:BaseUrl`，但需要确认是否正确

2. **环境变量格式问题**:
   - `.env` 文件中的 `OPENAI_BASE_URL` 和 `OPENAI_MODEL_ID` 不会被自动加载到 `Configuration`
   - 需要手动设置环境变量，或者修改代码直接从环境变量读取

## 建议的修复方案

### 方案 1: 直接从环境变量读取（推荐）
修改 `GetBaseUrl()` 和 `GetModelId()` 方法，直接从环境变量读取，而不是通过 `Configuration`:

```csharp
public string GetBaseUrl() => 
    Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";

public string GetModelId() => 
    Environment.GetEnvironmentVariable("OPENAI_MODEL_ID") ?? "deepseek-chat";
```

### 方案 2: 扩展 EmbeddedConfig
修改 Source Generator，让 `EmbeddedConfig` 也包含 `BaseUrl` 和 `ModelId`。

