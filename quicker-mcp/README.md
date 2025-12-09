# Quicker MCP Server

一个基于 Model Context Protocol (MCP) 的服务，允许 AI Agent 通过标准 MCP 协议调用 Quicker 的功能。

## SDK 选择

### 1. ModelContextProtocol（MCP .NET SDK）✅

**GitHub**：https://github.com/modelcontextprotocol/csharp-sdk  
**NuGet**：https://www.nuget.org/packages/ModelContextProtocol  
**安装**：
```bash
dotnet add package ModelContextProtocol --prerelease
```

**说明**：
- 官方 C# SDK，由 Microsoft 和 MCP 官方共同维护
- 当前为预览版本（需要 `--prerelease` 标志）
- 提供完整的 MCP 服务器和客户端实现
- 与 `Microsoft.Extensions.Hosting` 深度集成

**主要功能**：
- `AddMcpServer()` - 添加 MCP 服务器
- `WithStdioServerTransport()` - stdio 传输
- `[McpServerTool]` - 工具特性标记
- `WithToolsFromAssembly()` - 自动注册工具

### 2. StreamJsonRpc

**NuGet 包**：`StreamJsonRpc`  
**版本**：2.16.35  
**安装**：
```bash
dotnet add package StreamJsonRpc --version 2.16.35
```

**用途**：与 Quicker 服务进行 JSON-RPC 通信（通过命名管道）

### 3. Microsoft.Extensions.Hosting

**NuGet 包**：`Microsoft.Extensions.Hosting`  
**版本**：8.0.0  
**安装**：
```bash
dotnet add package Microsoft.Extensions.Hosting --version 8.0.0
```

**用途**：托管服务框架（MCP SDK 已集成，但可能需要显式引用）

### 4. Microsoft.Extensions.Logging

**NuGet 包**：
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

**版本**：8.0.0  
**安装**：
```bash
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 8.0.0
```

**用途**：日志记录（需要配置输出到 stderr）

### 5. Microsoft.Extensions.Configuration

**NuGet 包**：
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.EnvironmentVariables`

**版本**：8.0.0  
**安装**：
```bash
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables --version 8.0.0
```

**用途**：配置管理

### 6. QuickerExpressionAgent.Common

**引用方式**：项目引用
```xml
<ProjectReference Include="..\..\quicker-expression-agent\src\QuickerExpressionAgent.Common\QuickerExpressionAgent.Common.csproj" />
```

**用途**：复用 Quicker 通信相关的类型和接口
- `IQuickerService` 接口
- `VariableClass`、`ExpressionRequest`、`ExpressionResult` 等类型

## 快速开始

### 1. 安装 SDK

```bash
dotnet add package ModelContextProtocol --prerelease
dotnet add package Microsoft.Extensions.Hosting --version 8.0.0
dotnet add package StreamJsonRpc --version 2.16.35
```

### 2. 基本代码结构

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// 配置日志输出到 stderr（MCP 协议要求）
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// 添加 MCP 服务器
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

### 3. 定义工具

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class QuickerTools
{
    [McpServerTool, Description("Execute a C# expression")]
    public static async Task<string> ExecuteExpression(
        IQuickerService quickerService,
        [Description("The C# expression to execute")] string expression)
    {
        // 实现工具逻辑
        return "result";
    }
}
```

## 项目依赖

### 直接依赖
- **ModelContextProtocol**（--prerelease）- MCP SDK
- **StreamJsonRpc** 2.16.35 - JSON-RPC 通信
- **Microsoft.Extensions.*** 8.0.0 - 框架支持
- **QuickerExpressionAgent.Common** - 项目引用

### 间接依赖
- System.IO.Pipes（通过 StreamJsonRpc）

## 技术栈

- **.NET 8.0**：目标框架
- **ModelContextProtocol**：官方 MCP SDK
- **StreamJsonRpc**：JSON-RPC 通信
- **Microsoft.Extensions**：依赖注入、配置、日志

## 参考资源

- **MCP C# SDK GitHub**：https://github.com/modelcontextprotocol/csharp-sdk
- **MCP SDK NuGet**：https://www.nuget.org/packages/ModelContextProtocol
- **MCP 官方文档**：https://modelcontextprotocol.io
- **quicker-expression-agent**：参考 Quicker 通信实现

## 下一步

1. ✅ 已确认 MCP SDK：`ModelContextProtocol`（--prerelease）
2. 创建项目并安装所需 SDK
3. 实现 Quicker 服务连接器
4. 实现 MCP Tools
5. 测试和集成
