# Quicker Expression Agent

一个用于Quicker软件的Agent应用程序，可以将自然语言转换为C#表达式，并自动测试和优化。

## 功能特性

- 🤖 使用AI将自然语言转换为C#表达式
- 🔄 自动测试表达式并迭代优化
- 🔌 通过命名管道与Quicker服务通信
- 🛠️ 支持变量获取和设置
- 📝 交互式命令行界面

## 项目结构

```
quicker-expression-agent/
├── src/
│   └── QuickerExpressionAgent.Server/     # Agent Server (.NET 8.0)
│       ├── Agent/                         # Agent核心逻辑
│       ├── Communication/                 # IPC通信客户端
│       ├── Plugins/                       # Semantic Kernel插件
│       └── Program.cs                     # 主程序入口
├── RESEARCH.md                            # 技术调研文档
└── README.md                              # 本文档
```

## 前置要求

- .NET 8.0 SDK
- OpenAI API Key（或其他支持的LLM服务）
- Quicker软件（需要配合Quicker Service使用）

## 配置

1. 复制 `appsettings.json.example` 为 `appsettings.json`
2. 在 `appsettings.json` 中配置你的OpenAI API Key：

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "ModelId": "gpt-4-turbo-preview"
  }
}
```

或者设置环境变量：
```bash
export OPENAI_API_KEY=your-api-key-here
```

## 使用方法

### 交互式模式

```bash
dotnet run --project src/QuickerExpressionAgent.Server
```

或者：

```bash
dotnet run --project src/QuickerExpressionAgent.Server -- --interactive
```

### 单次生成模式

```bash
dotnet run --project src/QuickerExpressionAgent.Server -- --generate "计算两个数的和"
```

## 开发

### 构建项目

```bash
dotnet build
```

### 运行测试

```bash
dotnet test
```

## 架构说明

详见 [RESEARCH.md](RESEARCH.md)

## 许可证

MIT

