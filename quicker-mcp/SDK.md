# SDK 使用说明

## 核心 SDK

### ModelContextProtocol（MCP .NET SDK）

**GitHub 仓库**：https://github.com/modelcontextprotocol/csharp-sdk  
**NuGet 包名**：`ModelContextProtocol`  
**NuGet 地址**：https://www.nuget.org/packages/ModelContextProtocol  
**状态**：Preview 版本（需要 --prerelease 标志）

**安装命令**：
```bash
dotnet add package ModelContextProtocol --prerelease
```

**版本说明**：
- 当前为预览版本，可能会有破坏性变更
- 由 Microsoft 和 MCP 官方共同维护

**主要功能**：
- MCP 服务器基类（`McpServer`）
- MCP 客户端（`McpClient`）
- 工具注册和调用（`[McpServerTool]` 特性）
- stdio 传输支持（`StdioServerTransport`）
- 与 Microsoft.Extensions.Hosting 集成

**使用方式**：

**服务器端示例**：
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

[McpServerToolType]
public static class MyTools
{
    [McpServerTool, Description("Tool description")]
    public static string MyTool(string parameter) => "result";
}
```

**为什么使用 `static`？**

`McpServerToolType` 标记的类通常使用 `static` 的原因：

1. **工具方法的静态性**：
   - MCP 工具方法通常是无状态的函数式操作
   - 静态方法可以直接调用，无需实例化类
   - SDK 通过反射直接调用静态方法，无需创建实例

2. **避免不必要的实例化**：
   - 静态类无法被实例化，避免创建不必要的对象
   - 节省资源，提高性能

3. **代码清晰性**：
   - 明确传达该类仅用于提供工具方法的意图
   - 增强代码的可读性和维护性

4. **依赖注入支持**：
   - 虽然类是静态的，但工具方法可以通过参数注入依赖
   - 例如：`McpServer`、`HttpClient`、`ILogger` 等可以通过方法参数注入

**示例：带依赖注入的静态工具**：
```csharp
[McpServerToolType]
public static class QuickerTools
{
    [McpServerTool, Description("Execute a C# expression")]
    public static async Task<string> ExecuteExpression(
        McpServer thisServer,           // 注入 MCP 服务器
        IQuickerService quickerService,  // 注入 Quicker 服务
        HttpClient httpClient,          // 注入 HTTP 客户端
        [Description("The expression to execute")] string expression)
    {
        // 使用注入的服务
        return await quickerService.TestExpressionAsync(expression);
    }
}
```

**非静态类的使用**：

虽然推荐使用 `static`，但**非静态类也是完全支持的**。非静态类适用于以下场景：

1. **需要构造函数注入依赖**：
   - 通过构造函数注入服务，而不是通过方法参数
   - 适合需要多个工具方法共享同一依赖的场景

2. **需要维护实例状态**：
   - 如果工具需要维护状态（如缓存、计数器等）
   - 非静态类可以保存实例字段

3. **更好的单元测试**：
   - 非静态类更容易进行单元测试和模拟

**非静态类示例**：
```csharp
[McpServerToolType]
public class QuickerTools
{
    private readonly IQuickerService _quickerService;
    private readonly ILogger<QuickerTools> _logger;

    // 通过构造函数注入依赖
    public QuickerTools(
        IQuickerService quickerService,
        ILogger<QuickerTools> logger)
    {
        _quickerService = quickerService;
        _logger = logger;
    }

    [McpServerTool, Description("Execute a C# expression")]
    public async Task<string> ExecuteExpression(
        [Description("The expression to execute")] string expression)
    {
        _logger.LogInformation("Executing expression: {Expression}", expression);
        return await _quickerService.TestExpressionAsync(expression);
    }

    [McpServerTool, Description("Get variable value")]
    public async Task<string> GetVariable(
        [Description("Variable name")] string name)
    {
        var variable = await _quickerService.GetVariableAsync(name);
        return variable?.Value ?? "Not found";
    }
}
```

**静态类 vs 非静态类对比**：

| 特性 | 静态类 | 非静态类 |
|------|--------|----------|
| 实例化 | 不需要 | 需要（由 SDK 管理） |
| 依赖注入 | 通过方法参数 | 通过构造函数或方法参数 |
| 状态管理 | 不支持 | 支持（实例字段） |
| 性能 | 略好（无实例化开销） | 略差（需要实例化） |
| 适用场景 | 无状态工具 | 需要状态或构造函数注入 |

**选择建议**：
- **使用静态类**：工具方法无状态，依赖通过方法参数注入
- **使用非静态类**：需要构造函数注入、需要维护状态、或需要更好的测试性

**自动工具发现（索引工具）**：

MCP SDK 提供了通过特性标记和程序集扫描的方式，**自动发现和注册工具**，无需手动注册每个工具。这种方式可以显著减少工具定义的工作量。

**工作原理**：

1. **特性标记**：
   - 使用 `[McpServerToolType]` 标记工具类
   - 使用 `[McpServerTool]` 标记工具方法

2. **自动扫描**：
   - `WithToolsFromAssembly()` 方法会扫描程序集
   - 自动发现所有带有 `[McpServerToolType]` 特性的类
   - 自动注册这些类中带有 `[McpServerTool]` 特性的方法

3. **零配置注册**：
   - 无需手动调用注册方法
   - 只需定义工具类和方法，SDK 自动处理

**示例：自动工具发现**：

```csharp
// 1. 配置服务器（只需一行代码）
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();  // 自动扫描并注册所有工具

// 2. 定义工具（只需特性标记，无需手动注册）
[McpServerToolType]
public static class ExpressionTools
{
    [McpServerTool, Description("Execute a C# expression")]
    public static string ExecuteExpression(string expression) => "result";
}

[McpServerToolType]
public static class VariableTools
{
    [McpServerTool, Description("Get variable value")]
    public static string GetVariable(string name) => "value";
    
    [McpServerTool, Description("Set variable value")]
    public static void SetVariable(string name, string value) { }
}

// 3. 完成！所有工具已自动注册，无需额外代码
```

**对比：手动注册 vs 自动发现**：

**手动注册方式**（不推荐）：
```csharp
// 需要手动注册每个工具
var options = new McpServerOptions
{
    Handlers = new McpServerHandlers
    {
        ListToolsHandler = (request, ct) =>
        {
            return ValueTask.FromResult(new ListToolsResult
            {
                Tools = new[]
                {
                    new Tool { Name = "execute_expression", ... },
                    new Tool { Name = "get_variable", ... },
                    new Tool { Name = "set_variable", ... },
                    // ... 每个工具都要手动定义
                }
            });
        },
        CallToolHandler = (request, ct) =>
        {
            // 需要手动路由到对应的工具方法
            if (request.Params?.Name == "execute_expression") { ... }
            else if (request.Params?.Name == "get_variable") { ... }
            // ... 大量重复代码
        }
    }
};
```

**自动发现方式**（推荐）：
```csharp
// 只需一行配置
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();  // 自动处理所有工具

// 只需定义工具类和方法
[McpServerToolType]
public static class MyTools
{
    [McpServerTool]
    public static string MyTool(string param) => "result";
}
```

**优势**：

1. **减少代码量**：
   - 无需手动注册每个工具
   - 无需手动路由工具调用
   - 代码量减少 70% 以上

2. **提高可维护性**：
   - 添加新工具只需定义类和方法
   - 工具定义更加直观和集中
   - 减少人为错误

3. **增强一致性**：
   - 所有工具使用统一的注册方式
   - 自动处理工具元数据（名称、描述、参数等）

4. **支持 XML 注释**：
   - 如果工具方法是 `partial`，可以使用 XML 注释
   - SDK 会自动将 XML 注释转换为 `[Description]` 特性

**高级用法：指定程序集**：

```csharp
// 扫描指定程序集
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(MyTools).Assembly);  // 指定程序集

// 扫描多个程序集
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(ExpressionTools).Assembly)
    .WithToolsFromAssembly(typeof(VariableTools).Assembly);
```

**动态工具发现（按需搜索工具）**：

如果需要实现**动态工具发现机制**，让 Agent 可以主动搜索和发现工具，而不是一开始就提供所有工具的描述，可以使用以下方式：

**实现思路**：

1. **初始只暴露搜索工具**：
   - 只注册 `search_tools` 工具
   - Agent 可以通过这个工具搜索其他工具

2. **动态工具列表**：
   - 维护一个工具注册表
   - `search_tools` 可以根据关键词搜索并返回匹配的工具

3. **按需调用**：
   - Agent 先调用 `search_tools` 发现需要的工具
   - 然后根据返回的工具信息调用具体工具

**实现示例**：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

// 注册所有工具到内部注册表（但不直接暴露）
var toolRegistry = new ToolRegistry();
toolRegistry.RegisterAllToolsFromAssembly();

// 配置 MCP 服务器，但只暴露 search_tools
builder.Services
    .AddMcpServer(options =>
    {
        // 自定义工具列表处理器
        options.Handlers.ListToolsHandler = (request, ct) =>
        {
            // 只返回 search_tools，不返回其他工具
            return ValueTask.FromResult(new ListToolsResult
            {
                Tools = new[]
                {
                    new Tool
                    {
                        Name = "search_tools",
                        Description = "Search for available tools by keyword or description",
                        InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                            {
                                "type": "object",
                                "properties": {
                                    "keyword": {
                                        "type": "string",
                                        "description": "Keyword to search for in tool names or descriptions"
                                    },
                                    "category": {
                                        "type": "string",
                                        "description": "Optional category filter (e.g., 'expression', 'variable')"
                                    }
                                },
                                "required": ["keyword"]
                            }
                            """)
                    }
                }
            });
        };
        
        // 工具调用处理器
        options.Handlers.CallToolHandler = async (request, ct) =>
        {
            if (request.Params?.Name == "search_tools")
            {
                // 处理搜索请求
                return await HandleSearchTools(request, toolRegistry, ct);
            }
            
            // 其他工具调用（Agent 通过 search_tools 发现后调用）
            return await toolRegistry.CallToolAsync(request.Params?.Name, request.Params?.Arguments, ct);
        };
    })
    .WithStdioServerTransport();

await builder.Build().RunAsync();

// 搜索工具处理器
static async ValueTask<CallToolResult> HandleSearchTools(
    CallToolRequest request,
    ToolRegistry toolRegistry,
    CancellationToken cancellationToken)
{
    var keyword = request.Params?.Arguments?.TryGetValue("keyword", out var kw) == true 
        ? kw?.ToString()?.ToLowerInvariant() ?? "" 
        : "";
    
    var category = request.Params?.Arguments?.TryGetValue("category", out var cat) == true 
        ? cat?.ToString()?.ToLowerInvariant() 
        : null;
    
    // 搜索匹配的工具
    var matchingTools = toolRegistry.SearchTools(keyword, category);
    
    // 返回工具列表（包含名称、描述、参数等信息）
    var result = new StringBuilder();
    result.AppendLine($"Found {matchingTools.Count} matching tools:\n");
    
    foreach (var tool in matchingTools)
    {
        result.AppendLine($"**{tool.Name}**");
        result.AppendLine($"  Description: {tool.Description}");
        result.AppendLine($"  Parameters: {string.Join(", ", tool.Parameters)}");
        result.AppendLine();
    }
    
    return new CallToolResult
    {
        Content = new[] { new TextContentBlock { Text = result.ToString(), Type = "text" } }
    };
}

// 工具注册表
public class ToolRegistry
{
    private readonly List<ToolInfo> _tools = new();
    
    public void RegisterAllToolsFromAssembly()
    {
        // 扫描程序集，注册所有工具（但不直接暴露）
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
            {
                foreach (var method in type.GetMethods())
                {
                    var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                    if (toolAttr != null)
                    {
                        _tools.Add(new ToolInfo
                        {
                            Name = toolAttr.Name ?? method.Name,
                            Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                            Method = method,
                            Type = type
                        });
                    }
                }
            }
        }
    }
    
    public List<ToolInfo> SearchTools(string keyword, string? category = null)
    {
        return _tools.Where(t =>
            (string.IsNullOrEmpty(keyword) || 
             t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
             t.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)) &&
            (category == null || t.Name.Contains(category, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }
    
    public async ValueTask<CallToolResult> CallToolAsync(string? name, Dictionary<string, object?>? arguments, CancellationToken ct)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == name);
        if (tool == null)
        {
            throw new McpProtocolException($"Tool '{name}' not found", McpErrorCode.InvalidRequest);
        }
        
        // 调用工具方法
        // ... 实现调用逻辑
    }
}

// 工具信息
public class ToolInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public MethodInfo Method { get; set; } = null!;
    public Type Type { get; set; } = null!;
    public List<string> Parameters => Method.GetParameters()
        .Select(p => p.Name ?? "")
        .ToList();
}

// 实际工具定义（Agent 通过 search_tools 发现）
[McpServerToolType]
public static class ExpressionTools
{
    [McpServerTool, Description("Execute a C# expression")]
    public static string ExecuteExpression(string expression) => "result";
}

[McpServerToolType]
public static class VariableTools
{
    [McpServerTool, Description("Get variable value")]
    public static string GetVariable(string name) => "value";
}
```

**简化实现方式**（推荐）：

如果不需要完全隐藏工具列表，可以使用 MCP 协议的 `tools/list_changed` 通知机制：

```csharp
[McpServerToolType]
public static class DiscoveryTools
{
    [McpServerTool, Description("Search for tools by keyword or category")]
    public static async Task<string> SearchTools(
        McpServer server,
        [Description("Keyword to search")] string keyword,
        [Description("Optional category filter")] string? category = null)
    {
        // 获取所有可用工具
        var allTools = await server.ListToolsAsync();
        
        // 过滤匹配的工具
        var matching = allTools.Where(t =>
            t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        );
        
        if (!string.IsNullOrEmpty(category))
        {
            matching = matching.Where(t => 
                t.Name.Contains(category, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        // 返回工具信息
        var result = new StringBuilder();
        foreach (var tool in matching)
        {
            result.AppendLine($"**{tool.Name}**");
            result.AppendLine($"  {tool.Description}");
            result.AppendLine();
        }
        
        return result.ToString();
    }
}
```

**使用流程**：

1. **初始状态**：
   - Agent 只看到 `search_tools` 工具
   - 不知道其他工具的存在

2. **主动探索**：
   - Agent 调用 `search_tools("expression")` 
   - 返回匹配的工具列表

3. **按需调用**：
   - Agent 根据搜索结果调用具体工具
   - 例如：调用 `execute_expression` 工具

**优势**：

1. **减少初始提示大小**：不在一开始就列出所有工具
2. **按需发现**：Agent 只在需要时搜索相关工具
3. **更好的扩展性**：添加新工具不影响现有 Agent
4. **语义搜索**：可以根据任务需求搜索相关工具

**关键 API**：
- `AddMcpServer()` - 添加 MCP 服务器服务
- `WithStdioServerTransport()` - 配置 stdio 传输
- `WithToolsFromAssembly()` - 从程序集自动注册工具（**核心方法**）
- `[McpServerTool]` - 标记工具方法
- `[McpServerToolType]` - 标记工具类型
- `options.Handlers.ListToolsHandler` - 自定义工具列表处理器
- `options.Handlers.CallToolHandler` - 自定义工具调用处理器

**参考资源**：
- GitHub：https://github.com/modelcontextprotocol/csharp-sdk
- NuGet：https://www.nuget.org/packages/ModelContextProtocol
- 示例代码：仓库中的 `samples` 目录
- 文档：仓库中的 `docs` 目录

## 通信 SDK

### StreamJsonRpc

**包名**：`StreamJsonRpc`  
**版本**：2.16.35  
**用途**：与 Quicker 服务进行 JSON-RPC 通信

**使用场景**：
- 通过命名管道连接 Quicker 服务
- 调用 `IQuickerService` 接口方法

**参考实现**：
- 复用 `quicker-expression-agent` 中的 `QuickerServerClientConnector`

## 框架 SDK

### Microsoft.Extensions.Hosting

**包名**：`Microsoft.Extensions.Hosting`  
**版本**：8.0.0  
**用途**：托管服务框架

**使用场景**：
- 管理 `QuickerServiceConnector` 作为后台服务
- 依赖注入容器
- MCP SDK 需要此包进行集成

**注意**：MCP SDK 已经集成了 Hosting，可以直接使用 `Host.CreateApplicationBuilder`

### Microsoft.Extensions.Logging

**包名**：
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

**版本**：8.0.0  
**用途**：日志记录

**配置示例**：
```csharp
builder.Logging.AddConsole(consoleLogOptions =>
{
    // 配置所有日志输出到 stderr（MCP 协议要求）
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

### Microsoft.Extensions.Configuration

**包名**：
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.EnvironmentVariables`

**版本**：8.0.0  
**用途**：配置管理

## 项目引用

### QuickerExpressionAgent.Common

**路径**：`../quicker-expression-agent/src/QuickerExpressionAgent.Common/QuickerExpressionAgent.Common.csproj`

**包含内容**：
- `IQuickerService` - Quicker 服务接口
- `VariableClass` - 变量类型
- `ExpressionRequest` - 表达式请求
- `ExpressionResult` - 表达式结果
- `Constants` - 命名管道名称等常量

**复用方式**：直接项目引用，无需修改

## SDK 安装命令汇总

```bash
# MCP SDK（核心）
dotnet add package ModelContextProtocol --prerelease

# 框架 SDK（MCP SDK 可能需要，但通常已包含）
dotnet add package Microsoft.Extensions.Hosting --version 8.0.0
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables --version 8.0.0

# 通信 SDK
dotnet add package StreamJsonRpc --version 2.16.35
```

## 项目依赖关系

```
QuickerMcpServer
├── ModelContextProtocol (--prerelease)
│   └── Microsoft.Extensions.Hosting (自动包含)
├── StreamJsonRpc 2.16.35
├── Microsoft.Extensions.Logging 8.0.0
├── Microsoft.Extensions.Configuration 8.0.0
└── QuickerExpressionAgent.Common (项目引用)
```

## 开发注意事项

1. **Preview 版本**：
   - MCP SDK 当前为预览版本，可能会有破坏性变更
   - 建议关注 GitHub 仓库的更新

2. **日志配置**：
   - MCP 协议要求日志输出到 stderr
   - 需要配置 `LogToStandardErrorThreshold`

3. **工具注册**：
   - 可以使用 `[McpServerTool]` 特性自动注册工具
   - 也可以手动配置工具处理器

4. **依赖注入**：
   - MCP SDK 支持依赖注入
   - 工具方法可以注入 `McpServer`、`HttpClient` 等服务
