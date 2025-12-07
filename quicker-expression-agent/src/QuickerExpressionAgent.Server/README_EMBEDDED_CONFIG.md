# 嵌入配置说明

## 概述

此方案允许在编译时将 API key 从 `.env` 文件读取并生成到 C# 代码中，这样 API key 会直接编译到 exe 中，可以被加密工具加密。

## 使用方法

### 1. 创建 .env 文件

在项目根目录（`src/QuickerExpressionAgent.Server/`）创建 `.env` 文件：

```
# DeepSeek API Key (also supports OPENAI_API_KEY for backward compatibility)
DEEPSEEK_API_KEY=your-deepseek-api-key-here

# GLM (Zhipu) API Key
GLM_API_KEY=your-glm-api-key-here
```

**注意**：生成器只管理 API key。BaseUrl 和 ModelId 在 `ConfigurationService.cs` 中硬编码，不需要在 `.env` 文件中配置。

### 2. 确保文件被忽略

`.env` 文件已经在 `.gitignore` 中，不会被提交到 git。

### 3. 编译项目

编译时，MSBuild 会自动：
1. 读取 `.env` 文件
2. 生成 `Generated/EmbeddedConfig.cs` 文件
3. 将配置值编译到代码中

### 4. 加密 exe

编译完成后，使用你的加密工具对生成的 exe 进行加密。由于 API key 已经编译到代码中，加密工具会同时加密 API key。

## 工作原理

1. **编译时生成**：Source Generator 在编译时读取 `.env` 文件
2. **读取 API Key**：从 `.env` 文件读取 API key（支持 `KEY=VALUE` 格式）
3. **生成代码**：生成 `EmbeddedConfig.generated.cs` 静态类，只包含 API key
4. **编译到 exe**：生成的代码会被编译到 exe 中
5. **运行时读取**：`ConfigurationService` 优先使用 `EmbeddedConfig` 中的 API key，BaseUrl 和 ModelId 在代码中硬编码

## .env 文件格式

生成器只管理 API key，BaseUrl 和 ModelId 在 `ConfigurationService.cs` 中硬编码。

### API Keys（可选，至少配置一个）
- `DEEPSEEK_API_KEY=your-key` - DeepSeek API Key（用于 DeepSeek 模型）
- `GLM_API_KEY=your-key` - GLM (Zhipu) API Key（用于 GLM 模型）
- `OPENAI_API_KEY=your-key` - 向后兼容，等同于 DEEPSEEK_API_KEY
- `ZHIPU_API_KEY=your-key` - GLM API Key 的别名

### 其他格式支持
- 支持带引号的值：`DEEPSEEK_API_KEY="your-key"` 或 `DEEPSEEK_API_KEY='your-key'`
- 支持注释（以 `#` 开头）：`# This is a comment`

### BaseUrl 和 ModelId
- BaseUrl 和 ModelId 在 `ConfigurationService.cs` 中硬编码，不需要在 `.env` 文件中配置
- GLM 模型的 BaseUrl 固定为 `https://open.bigmodel.cn/api/paas/v4`
- 其他模型的 BaseUrl 在 `GetConfig()` 方法中定义

## 优先级

配置读取优先级（从高到低）：
1. `EmbeddedConfig`（编译时生成的代码）
2. `appsettings.json`（文件系统，开发时使用）
3. 环境变量

## 多 API Key 支持

现在支持在 `.env` 文件中配置多个 API Key：

- **DEEPSEEK_API_KEY**: 用于 DeepSeek 模型
- **GLM_API_KEY**: 用于 GLM (Zhipu) 模型（glm-4.5, glm-4.5-air, glm-4.6）

在 `GetBuiltInConfigs()` 方法中：
- GLM 模型配置会自动使用 `GLM_API_KEY`
- 其他模型配置使用 `DEEPSEEK_API_KEY`（或 `OPENAI_API_KEY`）

如果某个 API Key 未配置，对应的配置将使用空字符串，运行时可能会失败。建议至少配置一个 API Key。

## 注意事项

- `.env` 文件不会被提交到 git（已在 `.gitignore` 中）
- 如果 `.env` 文件不存在，会使用默认值（空 API key）
- 生成的 `EmbeddedConfig.cs` 文件是自动生成的，不要手动编辑
- 修改 `.env` 文件后需要重新编译才能生效

