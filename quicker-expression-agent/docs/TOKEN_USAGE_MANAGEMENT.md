# Token 使用情况管理和上下文长度控制

## 问题分析

### Token 消耗增长趋势

随着对话进行，每次 API 调用的 token 消耗会逐渐增加：

```
第 1 次调用：
  - System Message: ~600 tokens（只在第一次发送）
  - Chat History: 0 tokens
  - Tools: ~2500 tokens（每次调用都发送）
  - Total: ~3100 tokens

第 5 次调用：
  - System Message: 0 tokens（不重复）
  - Chat History: ~2000 tokens（累积的对话）
  - Tools: ~2500 tokens
  - Total: ~4500 tokens

第 10 次调用：
  - System Message: 0 tokens
  - Chat History: ~5000 tokens（更多累积）
  - Tools: ~2500 tokens
  - Total: ~7500 tokens
```

### 关键发现

1. **聊天历史会持续累积**：每次调用都会发送完整的 `_chatHistory`
2. **工具定义每次都会发送**：固定成本约 2000-3000 tokens
3. **没有内置的 token 使用情况查看方法**：Semantic Kernel 不直接提供 token 计数

## Semantic Kernel 的限制

**Semantic Kernel 目前没有直接提供查看 token 使用情况的方法**。但是可以通过以下方式估算和管理：

### 1. 估算 Token 数量

可以使用简单的估算方法：
- 英文：1 token ≈ 4 个字符
- 中文：1 token ≈ 1.5 个字符
- 工具定义：固定大小（需要手动计算）

### 2. 监控聊天历史长度

```csharp
// 监控消息数量
int messageCount = _chatHistory.Count;

// 估算总字符数
int totalChars = _chatHistory.Sum(msg => msg.Content?.Length ?? 0);
int estimatedTokens = totalChars / 3; // 粗略估算
```

### 3. 实现历史消息管理

#### 方案 A：保留最近 N 条消息

```csharp
private void TrimChatHistory(int maxMessages = 20)
{
    if (_chatHistory.Count > maxMessages)
    {
        // 保留系统消息和最近的 N 条消息
        var systemMessages = _chatHistory.Where(m => m.Role == AuthorRole.System).ToList();
        var recentMessages = _chatHistory
            .Where(m => m.Role != AuthorRole.System)
            .TakeLast(maxMessages)
            .ToList();
        
        _chatHistory.Clear();
        _chatHistory.AddRange(systemMessages);
        _chatHistory.AddRange(recentMessages);
    }
}
```

#### 方案 B：按 Token 数量限制

```csharp
private void TrimChatHistoryByTokens(int maxTokens = 8000)
{
    int currentTokens = EstimateTokenCount(_chatHistory);
    
    if (currentTokens > maxTokens)
    {
        // 保留系统消息
        var systemMessages = _chatHistory.Where(m => m.Role == AuthorRole.System).ToList();
        
        // 从后往前删除消息，直到 token 数量在限制内
        var otherMessages = _chatHistory
            .Where(m => m.Role != AuthorRole.System)
            .ToList();
        
        _chatHistory.Clear();
        _chatHistory.AddRange(systemMessages);
        
        int tokens = EstimateTokenCount(systemMessages);
        foreach (var msg in otherMessages.Reverse())
        {
            int msgTokens = EstimateTokenCount(msg);
            if (tokens + msgTokens > maxTokens)
                break;
            
            _chatHistory.Insert(systemMessages.Count, msg);
            tokens += msgTokens;
        }
    }
}

private int EstimateTokenCount(ChatHistory history)
{
    int totalChars = 0;
    foreach (var msg in history)
    {
        totalChars += msg.Content?.Length ?? 0;
        // 工具调用也会占用 token，需要额外估算
    }
    return totalChars / 3; // 粗略估算
}
```

#### 方案 C：摘要旧消息

```csharp
private async Task SummarizeOldMessages()
{
    if (_chatHistory.Count > 30)
    {
        // 获取旧消息（除了系统消息和最近 10 条）
        var oldMessages = _chatHistory
            .Where(m => m.Role != AuthorRole.System)
            .SkipLast(10)
            .ToList();
        
        // 使用 AI 生成摘要
        var summary = await GenerateSummary(oldMessages);
        
        // 替换旧消息为摘要
        _chatHistory.RemoveRange(
            _chatHistory.IndexOf(oldMessages.First()),
            oldMessages.Count
        );
        _chatHistory.Insert(
            _chatHistory.Count - 10,
            new ChatMessageContent(AuthorRole.System, summary)
        );
    }
}
```

## 推荐方案

### 短期方案：消息数量限制

在每次调用前检查并截断历史：

```csharp
// 在 GenerateExpressionAsync 开始时
if (_chatHistory.Count > 30) // 保留最近 30 条消息
{
    TrimChatHistory(30);
}
```

### 长期方案：Token 估算 + 智能截断

实现更精确的 token 估算和智能截断机制。

## 注意事项

1. **工具定义无法减少**：每次调用都会发送，这是固定成本
2. **系统提示词只在第一次发送**：后续调用不重复
3. **流式响应**：在流式响应中，token 使用情况可能在响应完成后才能获取
4. **OpenAI API 响应**：实际的 token 使用情况在 API 响应中，但 Semantic Kernel 可能不直接暴露

## 未来改进

可以考虑：
1. 实现 token 估算工具类
2. 添加聊天历史管理功能
3. 提供配置选项（最大消息数、最大 token 数等）
4. 实现消息摘要功能

