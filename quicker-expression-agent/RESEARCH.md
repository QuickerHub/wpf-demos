# Quicker Expression Agent 技术调研方案

## 项目概述

开发一个用于Quicker软件的Agent应用程序，主要功能：
- 实现自然语言编写类C#语法的表达式
- 自主调用工具进行测试和反复修改
- 使用.NET 8.0+技术栈开发Agent Server
- 通过进程通信直接调用Quicker中注入的方法

## 技术架构方案

### 1. Agent Server 技术栈

#### 1.1 核心框架：Microsoft Semantic Kernel

**推荐理由：**
- 微软官方AI Agent框架，与.NET 8.0深度集成
- 支持工具调用（Tools/Plugins）机制
- 支持多种LLM后端（OpenAI, Azure OpenAI, Anthropic等）
- 提供Agent架构和编排能力
- 活跃的社区和文档支持

**关键包：**
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.0.0" />
```

**核心功能：**
- `Kernel` - 核心执行引擎
- `KernelFunction` - 函数定义和执行
- `KernelPlugin` - 插件系统（工具集合）
- `KernelAgent` - Agent编排
- `KernelArguments` - 参数传递

#### 1.2 LLM后端选择

**推荐方案：**
1. **OpenAI GPT-4/GPT-4 Turbo** - 代码生成能力强
2. **Azure OpenAI** - 企业级部署，数据安全
3. **Anthropic Claude** - 长上下文支持，代码理解好

**配置示例：**
```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId: "gpt-4-turbo-preview",
        apiKey: apiKey)
    .Build();
```

### 2. 进程间通信（IPC）架构

#### 2.1 架构设计

采用简单的RPC调用方式，在Quicker中注入服务，Agent Server作为客户端直接调用方法：

```
┌─────────────────┐         IPC          ┌──────────────────┐
│  Agent Server   │ ◄──────────────────► │ Quicker Service  │
│  (Client)       │   Named Pipe/TCP     │ (Injected)       │
└─────────────────┘                       └──────────────────┘
```

#### 2.2 通信协议设计

使用简化的JSON-RPC风格消息格式，但不完全遵循MCP标准：

**请求消息格式：**
```json
{
  "id": 1,
  "method": "ExecuteExpression",
  "params": {
    "expression": "1 + 2",
    "variables": {}
  }
}
```

**响应消息格式：**
```json
{
  "id": 1,
  "result": {
    "success": true,
    "value": 3,
    "error": null
  }
}
```

**错误响应格式：**
```json
{
  "id": 1,
  "error": {
    "code": -32000,
    "message": "表达式执行失败",
    "data": "具体错误信息"
  }
}
```

**通信方式选择：**

1. **命名管道（Named Pipes）** - 推荐
   - Windows平台原生支持
   - 性能好，延迟低
   - 适合本地进程通信
   ```csharp
   using var pipeClient = new NamedPipeClientStream(
       ".", "QuickerMCPServer", PipeDirection.InOut);
   await pipeClient.ConnectAsync();
   ```

2. **TCP Socket**
   - 跨平台支持
   - 可扩展性强
   - 适合远程通信场景

3. **gRPC**
   - 高性能RPC框架
   - 强类型定义
   - 需要定义.proto文件

### 3. 进程间通信（IPC）实现

#### 3.1 命名管道实现

**Server端（Quicker注入）：**
```csharp
public class QuickerExpressionService
{
    private NamedPipeServerStream _pipeServer;
    private readonly IActionContext _context;
    
    public QuickerExpressionService(IActionContext context)
    {
        _context = context;
    }
    
    public async Task StartAsync()
    {
        _pipeServer = new NamedPipeServerStream(
            "QuickerExpressionService",
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
        
        await _pipeServer.WaitForConnectionAsync();
        await ProcessMessagesAsync();
    }
    
    private async Task ProcessMessagesAsync()
    {
        var buffer = new byte[4096];
        var bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length);
        var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        var request = JsonSerializer.Deserialize<RpcRequest>(requestJson);
        var response = await HandleRequestAsync(request);
        
        var responseJson = JsonSerializer.Serialize(response);
        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
        await _pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length);
    }
    
    private async Task<RpcResponse> HandleRequestAsync(RpcRequest request)
    {
        return request.Method switch
        {
            "ExecuteExpression" => await ExecuteExpressionAsync(request),
            "GetVariable" => await GetVariableAsync(request),
            "SetVariable" => await SetVariableAsync(request),
            _ => new RpcResponse 
            { 
                Id = request.Id, 
                Error = new RpcError { Message = "Unknown method" } 
            }
        };
    }
    
    private async Task<RpcResponse> ExecuteExpressionAsync(RpcRequest request)
    {
        var @params = request.Params;
        var expression = @params.GetProperty("expression").GetString();
        var variables = @params.GetProperty("variables").Deserialize<Dictionary<string, object>>();
        
        try
        {
            // 使用Quicker的表达式执行器
            var eval = new EvalContext();
            var result = ExpressionRunner.RunExpression(
                _context, eval, expression, false);
            
            return new RpcResponse
            {
                Id = request.Id,
                Result = new { success = true, value = result }
            };
        }
        catch (Exception ex)
        {
            return new RpcResponse
            {
                Id = request.Id,
                Error = new RpcError { Message = ex.Message }
            };
        }
    }
}
```

**Client端（Agent Server）：**
```csharp
public class QuickerServiceClient
{
    private NamedPipeClientStream _pipeClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public async Task<T> CallAsync<T>(string method, object @params)
    {
        _pipeClient = new NamedPipeClientStream(
            ".", "QuickerExpressionService", PipeDirection.InOut);
        
        await _pipeClient.ConnectAsync();
        
        var request = new RpcRequest
        {
            Id = Guid.NewGuid().GetHashCode(),
            Method = method,
            Params = @params
        };
        
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        await _pipeClient.WriteAsync(requestBytes, 0, requestBytes.Length);
        
        // 读取响应
        var buffer = new byte[4096];
        var bytesRead = await _pipeClient.ReadAsync(buffer, 0, buffer.Length);
        var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var response = JsonSerializer.Deserialize<RpcResponse>(responseJson, JsonOptions);
        
        if (response.Error != null)
        {
            throw new Exception(response.Error.Message);
        }
        
        return JsonSerializer.Deserialize<T>(response.Result.GetRawText());
    }
    
    public async Task<ExpressionResult> ExecuteExpressionAsync(
        string expression, Dictionary<string, object> variables = null)
    {
        return await CallAsync<ExpressionResult>("ExecuteExpression", new
        {
            expression,
            variables = variables ?? new Dictionary<string, object>()
        });
    }
}
```

#### 3.2 消息序列化

使用 `System.Text.Json` 进行JSON序列化：
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
```

### 4. 自然语言到表达式转换

#### 4.1 转换流程

```
自然语言输入 
  ↓
LLM理解意图
  ↓
生成C#表达式
  ↓
语法验证
  ↓
执行测试
  ↓
反馈优化
```

#### 4.2 Semantic Kernel实现

**定义Prompt模板：**
```csharp
var prompt = """
将以下自然语言描述转换为C#表达式。
表达式应该符合Quicker的表达式语法规范。

自然语言：{{$input}}

要求：
1. 使用类C#语法
2. 支持变量引用（使用{变量名}格式）
3. 支持方法调用
4. 返回可执行的表达式代码

表达式：
""";

var function = kernel.CreateFunctionFromPrompt(prompt);
```

**工具调用示例：**
```csharp
// 定义测试工具
kernel.Plugins.AddFromType<ExpressionTestPlugin>();

// Agent调用
var agent = kernel.CreateAgent(
    instructions: "你是一个C#表达式生成专家...",
    plugins: kernel.Plugins);

var result = await agent.InvokeAsync("计算两个数的和");
```

#### 4.3 表达式验证和执行

**使用Roslyn进行语法检查：**
```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var syntaxTree = CSharpSyntaxTree.ParseText(expression);
var diagnostics = syntaxTree.GetDiagnostics();

if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
{
    // 报告错误，让LLM修正
}
```

**执行表达式（参考现有代码）：**
- 使用 `Z.Expressions.Eval`（Quicker现有方案）
- 或使用 `Microsoft.CodeAnalysis.CSharp.Scripting`

### 5. 自主测试和迭代

#### 5.1 测试工具插件

```csharp
public class ExpressionTestPlugin
{
    [KernelFunction]
    [Description("测试C#表达式是否正确执行")]
    public async Task<string> TestExpression(
        [Description("要测试的表达式")] string expression,
        [Description("测试参数")] Dictionary<string, object> variables)
    {
        try
        {
            // 调用Quicker服务执行表达式
            var result = await _quickerClient.ExecuteExpressionAsync(
                expression, variables);
            
            return $"测试成功：{result.Value}";
        }
        catch (Exception ex)
        {
            return $"测试失败：{ex.Message}";
        }
    }
}
```

#### 5.2 迭代优化流程

```csharp
public class ExpressionAgent
{
    public async Task<string> GenerateAndRefineExpressionAsync(
        string naturalLanguage, int maxIterations = 3)
    {
        string expression = null;
        string lastError = null;
        
        for (int i = 0; i < maxIterations; i++)
        {
            // 生成或优化表达式
            expression = await _kernel.InvokeAsync<string>(
                "generate_expression",
                new KernelArguments
                {
                    ["input"] = naturalLanguage,
                    ["previous_error"] = lastError ?? "",
                    ["previous_expression"] = expression ?? ""
                });
            
            // 测试表达式
            var testResult = await TestExpressionAsync(expression);
            
            if (testResult.Success)
            {
                return expression;
            }
            
            lastError = testResult.Error;
        }
        
        return expression; // 返回最后一次尝试的结果
    }
}
```

### 6. 项目结构

```
quicker-expression-agent/
├── src/
│   ├── QuickerExpressionAgent.Server/     # Agent Server主程序 (.NET 8.0)
│   │   ├── Program.cs
│   │   ├── Agent/
│   │   │   ├── ExpressionAgent.cs
│   │   │   └── ExpressionGenerator.cs
│   │   ├── Communication/
│   │   │   ├── QuickerServiceClient.cs
│   │   │   ├── RpcRequest.cs
│   │   │   └── RpcResponse.cs
│   │   ├── Plugins/
│   │   │   ├── ExpressionTestPlugin.cs
│   │   │   └── QuickerToolsPlugin.cs
│   │   └── QuickerExpressionAgent.Server.csproj
│   │
│   └── QuickerExpressionAgent.Service/    # Quicker注入的服务 (.NET Framework 4.7.2)
│       ├── QuickerExpressionService.cs
│       ├── Services/
│       │   ├── ExpressionExecutor.cs
│       │   └── VariableService.cs
│       ├── Models/
│       │   ├── RpcRequest.cs
│       │   └── RpcResponse.cs
│       └── QuickerExpressionAgent.Service.csproj
│
├── tests/
│   └── QuickerExpressionAgent.Tests/
│
├── README.md
└── RESEARCH.md (本文档)
```

### 7. 依赖包清单

**Agent Server (.NET 8.0):**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.0.0" />
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.8.0" />
</ItemGroup>
```

**Quicker Service (.NET Framework 4.7.2):**
```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <!-- 引用Quicker的DLL -->
  <Reference Include="Quicker.Public">
    <HintPath>C:\Program Files\Quicker\Quicker.Public.dll</HintPath>
  </Reference>
  <Reference Include="Z.Expressions.Eval">
    <HintPath>C:\Program Files\Quicker\Z.Expressions.Eval.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 8. 开发步骤

1. **Phase 1: 基础架构**
   - 创建Agent Server项目（.NET 8.0）
   - 创建Quicker Service项目（.NET Framework 4.7.2）
   - 实现命名管道IPC通信
   - 实现RPC消息格式和序列化

2. **Phase 2: Quicker服务集成**
   - 在Quicker中注入服务
   - 实现表达式执行方法
   - 实现变量获取/设置方法
   - 实现其他工具方法

3. **Phase 3: Agent功能**
   - 集成Semantic Kernel
   - 实现自然语言到表达式转换
   - 实现表达式测试插件

4. **Phase 4: 迭代优化**
   - 实现自动测试和反馈循环
   - 优化提示词和Agent指令
   - 性能优化

5. **Phase 5: 打包部署**
   - 配置自包含发布
   - 创建安装包
   - 编写使用文档

### 9. 关键技术点

#### 9.1 表达式语法兼容性

需要确保生成的表达式符合Quicker的表达式规范：
- 变量引用：`{变量名}`
- 方法调用：`MethodName(args)`
- 类型引用：需要先注册（参考现有RegistrationCommandParser）

#### 9.2 错误处理和反馈

- 捕获语法错误
- 捕获运行时错误
- 将错误信息反馈给LLM进行修正
- 提供用户友好的错误提示

#### 9.3 安全性考虑

- 表达式执行沙箱（如果需要）
- API密钥安全存储
- 输入验证和清理
- 防止代码注入攻击

### 10. 参考资源

- [Semantic Kernel文档](https://learn.microsoft.com/zh-cn/semantic-kernel/)
- [.NET命名管道](https://learn.microsoft.com/zh-cn/dotnet/standard/io/how-to-use-named-pipes-for-network-inter-process-communication)
- [Roslyn文档](https://learn.microsoft.com/zh-cn/dotnet/csharp/roslyn-sdk/)
- [JSON-RPC 2.0规范](https://www.jsonrpc.org/specification)

### 11. 替代方案

如果Semantic Kernel不满足需求，可以考虑：
- **LangChain.NET** - 另一个.NET Agent框架
- **AutoGen** - 多Agent协作框架
- **自定义实现** - 直接使用LLM API + 工具调用

## 总结

推荐使用 **Microsoft Semantic Kernel + 命名管道IPC + 直接方法注入** 的方案，因为：
1. Semantic Kernel是微软官方框架，与.NET集成好
2. 命名管道在Windows平台性能优秀，延迟低
3. 直接方法注入简单直接，无需遵循复杂协议标准
4. 技术栈统一，便于维护和调试
5. 可以根据实际需求灵活扩展方法接口

