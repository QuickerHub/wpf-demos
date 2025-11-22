# Semantic Kernel Token 估算方法调研

## 核心发现

**Semantic Kernel 本身没有提供内置的 token 估算方法**，但可以通过以下方式实现：

## 方法一：日志记录和计量功能（获取实际 Token 使用量）

### 概述
Semantic Kernel 的 `Microsoft.SemanticKernel.Connectors.AI.OpenAI` 包提供了对每次请求的 token 使用情况的日志记录和计量功能。

### 实现步骤

1. **安装必要的包**
   ```xml
   <PackageReference Include="Microsoft.SemanticKernel.Connectors.AI.OpenAI" />
   ```

2. **配置日志记录**
   - 集成 Azure Application Insights 或其他日志系统
   - 启用 Semantic Kernel 的日志记录功能

3. **可获取的指标**
   - `SemanticKernel.Connectors.OpenAI.PromptTokens`：提示 token 数量
   - `SemanticKernel.Connectors.OpenAI.CompletionTokens`：完成 token 数量
   - `SemanticKernel.Connectors.OpenAI.TotalTokens`：总 token 数量

### 优点
- ✅ 获取**实际**的 token 使用量（不是估算）
- ✅ 精确度高
- ✅ 可以用于成本分析和优化

### 缺点
- ❌ 需要配置日志系统（如 Application Insights）
- ❌ 只能在 API 调用**之后**获取，无法在调用前估算
- ❌ 对于流式响应，可能需要等待响应完成

### 参考资源
- [Track Your Token Usage and Costs with Semantic Kernel](https://devblogs.microsoft.com/semantic-kernel/track-your-token-usage-and-costs-with-semantic-kernel/)

## 方法二：使用 tiktoken 库（精确 Token 计数）

### 概述
使用 OpenAI 的 `tiktoken` 库（或 .NET 版本）可以精确计算文本的 token 数量。

### 实现方式

#### Python 版本（tiktoken）
```python
import tiktoken

encoding = tiktoken.encoding_for_model("gpt-4")
tokens = encoding.encode("Your text here")
token_count = len(tokens)
```

#### .NET 版本
目前 .NET 生态系统中没有官方的 tiktoken 实现，但有一些社区项目：
- `SharpToken`：.NET 的 tiktoken 实现
- `TiktokenSharp`：另一个 .NET 实现

### 实现示例（使用 SharpToken）

```csharp
using SharpToken;

public class TokenCounter
{
    private readonly GptEncoding _encoding;
    
    public TokenCounter()
    {
        // 根据模型选择编码
        _encoding = GptEncoding.GetEncodingForModel("gpt-4");
    }
    
    public int CountTokens(string text)
    {
        var tokens = _encoding.Encode(text);
        return tokens.Count;
    }
    
    public int CountTokens(ChatHistory history)
    {
        int totalTokens = 0;
        foreach (var message in history)
        {
            if (!string.IsNullOrEmpty(message.Content))
            {
                totalTokens += CountTokens(message.Content);
            }
            
            // 工具调用也会占用 token
            if (message.Items != null)
            {
                foreach (var item in message.Items)
                {
                    if (item is ChatMessageContent itemContent && !string.IsNullOrEmpty(itemContent.Content))
                    {
                        totalTokens += CountTokens(itemContent.Content);
                    }
                }
            }
        }
        return totalTokens;
    }
}
```

### 优点
- ✅ 精确度高（与 OpenAI API 使用的 tokenizer 一致）
- ✅ 可以在 API 调用**之前**估算
- ✅ 支持不同模型的编码方式

### 缺点
- ❌ 需要引入额外的 NuGet 包
- ❌ 需要知道使用的模型类型（gpt-3.5-turbo, gpt-4 等）
- ❌ 工具定义的 token 需要单独计算

### NuGet 包
- `SharpToken`：https://www.nuget.org/packages/SharpToken/
- `TiktokenSharp`：https://www.nuget.org/packages/TiktokenSharp/

## 方法三：简单字符估算（当前实现）

### 概述
使用字符数除以固定系数来估算 token 数量。

### 当前实现

```csharp
public int EstimateTokenCount()
{
    int totalChars = 0;
    
    foreach (var message in _chatHistory)
    {
        if (!string.IsNullOrEmpty(message.Content))
        {
            totalChars += message.Content.Length;
        }
        
        if (message.Items != null)
        {
            foreach (var item in message.Items)
            {
                if (item is ChatMessageContent itemContent && !string.IsNullOrEmpty(itemContent.Content))
                {
                    totalChars += itemContent.Content.Length;
                }
            }
        }
    }
    
    // 粗略估算：3 个字符 ≈ 1 token
    // - 英文：1 token ≈ 4 个字符
    // - 中文：1 token ≈ 1.5 个字符
    // - 混合内容：使用平均值 3 个字符/token
    return totalChars / 3;
}
```

### 估算规则
- **英文**：1 token ≈ 4 个字符
- **中文**：1 token ≈ 1.5 个字符
- **混合内容**：使用平均值 3 个字符/token（保守估算）

### 优点
- ✅ 实现简单，无需额外依赖
- ✅ 性能好，计算快速
- ✅ 可以实时估算

### 缺点
- ❌ 精度较低（误差可能达到 20-30%）
- ❌ 对于不同语言混合的内容，估算可能不准确
- ❌ 工具定义的 token 需要单独估算

## 方法四：从 API 响应中提取（流式响应）

### 概述
在流式响应完成后，从 API 响应中提取实际的 token 使用情况。

### 实现方式

```csharp
// 注意：这需要检查 ChatMessageContent 的 Metadata 属性
// 或者使用非流式 API 调用来获取 usage 信息

var result = await chatCompletion.GetChatMessageContentsAsync(
    _chatHistory,
    executionSettings: executionSettings,
    _kernel,
    cancellationToken);

// 检查 result 的 Metadata 或相关属性
// 注意：Semantic Kernel 可能不直接暴露这些信息
```

### 限制
- Semantic Kernel 的流式 API 可能不直接暴露 token 使用情况
- 需要检查 `ChatMessageContent` 的 `Metadata` 属性
- 可能需要使用底层的 OpenAI SDK 来获取

## 推荐方案

### 短期方案（当前实现）
使用**方法三：简单字符估算**
- 适合快速实现和实时显示
- 精度足够用于监控和预警
- 无需额外依赖

### 长期方案（精确计数）
使用**方法二：tiktoken/SharpToken**
- 需要精确 token 计数时
- 需要控制成本时
- 需要精确的上下文长度管理时

### 成本分析方案
使用**方法一：日志记录和计量功能**
- 用于成本分析和优化
- 用于生产环境的监控
- 需要历史数据统计时

## 工具定义的 Token 估算

工具定义（Function/Tool definitions）也会占用 token，需要单独估算：

```csharp
public int EstimateToolDefinitionsTokenCount(Kernel kernel)
{
    int toolTokens = 0;
    
    // 获取所有插件
    foreach (var plugin in kernel.Plugins)
    {
        foreach (var function in plugin)
        {
            // 估算每个函数的 token：
            // - 函数名
            // - 函数描述
            // - 参数定义（名称、类型、描述）
            // - 返回类型描述
            
            toolTokens += EstimateFunctionTokenCount(function);
        }
    }
    
    return toolTokens;
}

private int EstimateFunctionTokenCount(KernelFunction function)
{
    int tokens = 0;
    
    // 函数名和描述
    tokens += (function.Name?.Length ?? 0) / 3;
    tokens += (function.Description?.Length ?? 0) / 3;
    
    // 参数定义
    if (function.Metadata.Parameters != null)
    {
        foreach (var param in function.Metadata.Parameters)
        {
            tokens += (param.Name?.Length ?? 0) / 3;
            tokens += (param.Description?.Length ?? 0) / 3;
            tokens += (param.ParameterType?.Name?.Length ?? 0) / 3;
        }
    }
    
    return tokens;
}
```

## 实际 Token 消耗组成

每次 API 调用的 token 消耗包括：

1. **系统提示词**（System Message）
   - 只在第一次调用时发送
   - 通常 500-1000 tokens

2. **聊天历史**（Chat History）
   - 每次调用都会发送完整的 `_chatHistory`
   - 随着对话进行而增长

3. **工具定义**（Tool Definitions）
   - 每次调用都会发送所有工具的定义
   - 固定成本，通常 2000-3000 tokens

4. **当前用户消息**（User Message）
   - 当前轮次的用户输入

5. **AI 响应**（Completion）
   - 由 API 返回，不占用输入 token

## 改进建议

### 1. 集成 SharpToken 进行精确计数

```csharp
// 安装 SharpToken NuGet 包
// dotnet add package SharpToken

using SharpToken;

public class TokenEstimator
{
    private readonly GptEncoding _encoding;
    
    public TokenEstimator(string modelName = "gpt-4")
    {
        _encoding = GptEncoding.GetEncodingForModel(modelName);
    }
    
    public int CountTokens(ChatHistory history)
    {
        // 实现精确的 token 计数
    }
}
```

### 2. 缓存工具定义的 Token 数量

```csharp
private int? _cachedToolDefinitionsTokens;

public int GetToolDefinitionsTokenCount()
{
    if (_cachedToolDefinitionsTokens == null)
    {
        _cachedToolDefinitionsTokens = EstimateToolDefinitionsTokenCount(_kernel);
    }
    return _cachedToolDefinitionsTokens.Value;
}
```

### 3. 实现智能的上下文管理

```csharp
public void TrimChatHistoryIfNeeded(int maxTokens = 8000)
{
    int currentTokens = EstimateTokenCount();
    int toolTokens = GetToolDefinitionsTokenCount();
    int systemTokens = EstimateSystemMessageTokenCount();
    
    int availableTokens = maxTokens - toolTokens - systemTokens;
    
    if (currentTokens > availableTokens)
    {
        // 智能截断或摘要
        TrimChatHistory(availableTokens);
    }
}
```

## 参考资料

1. [Track Your Token Usage and Costs with Semantic Kernel](https://devblogs.microsoft.com/semantic-kernel/track-your-token-usage-and-costs-with-semantic-kernel/)
2. [SharpToken NuGet Package](https://www.nuget.org/packages/SharpToken/)
3. [OpenAI Tokenizer](https://platform.openai.com/tokenizer)
4. [tiktoken Python Library](https://github.com/openai/tiktoken)

