# 嵌入配置说明

## 概述

此方案允许在编译时将 API key 从 `.env` 文件读取并生成到 C# 代码中，这样 API key 会直接编译到 exe 中，可以被加密工具加密。

## 使用方法

### 1. 创建 .env 文件

在项目根目录（`src/QuickerExpressionAgent.Server/`）创建 `.env` 文件：

```
# OpenAI Configuration
OPENAI_API_KEY=your-actual-api-key-here
OPENAI_BASE_URL=https://api.deepseek.com
OPENAI_MODEL_ID=deepseek-chat
```

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

1. **编译时生成**：MSBuild 在编译前执行 `GenerateEmbeddedConfig` 任务
2. **读取配置**：从 `.env` 文件读取配置值（支持 `KEY=VALUE` 格式）
3. **生成代码**：生成 `EmbeddedConfig.cs` 静态类，包含配置值
4. **编译到 exe**：生成的代码会被编译到 exe 中
5. **运行时读取**：`ConfigurationService` 优先使用 `EmbeddedConfig` 中的值

## .env 文件格式

支持以下格式：
- `OPENAI_API_KEY=your-key`
- `OPENAI_BASE_URL=https://api.deepseek.com`
- `OPENAI_MODEL_ID=deepseek-chat`

也支持带引号的值：
- `OPENAI_API_KEY="your-key"`
- `OPENAI_API_KEY='your-key'`

支持注释（以 `#` 开头）：
- `# This is a comment`

## 优先级

配置读取优先级（从高到低）：
1. `EmbeddedConfig`（编译时生成的代码）
2. `appsettings.json`（文件系统，开发时使用）
3. 环境变量

## 注意事项

- `.env` 文件不会被提交到 git（已在 `.gitignore` 中）
- 如果 `.env` 文件不存在，会使用默认值（空 API key）
- 生成的 `EmbeddedConfig.cs` 文件是自动生成的，不要手动编辑
- 修改 `.env` 文件后需要重新编译才能生效

