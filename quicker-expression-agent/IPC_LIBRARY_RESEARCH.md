# .NET IPC 库调研报告

## 当前使用的库：H.Ipc

**优点：**
- 轻量级，简单易用
- 支持源生成器自动生成客户端代码
- 基于命名管道，性能较好
- 支持 MessagePack 序列化

**缺点：**
- 生成器在处理某些类型时存在 bug（如 `long`、`int?`、`List<T>?`）
- 功能相对简单，缺少高级特性
- 社区支持较少

## 推荐的替代方案

### 1. StreamJsonRpc ⭐⭐⭐⭐⭐

**GitHub:** https://github.com/microsoft/vs-streamjsonrpc

**特点：**
- 由 Microsoft 开发和维护
- 支持 JSON-RPC 2.0 协议
- 支持多种传输方式（命名管道、TCP、WebSocket、HTTP）
- 支持流式传输
- 强类型接口定义
- 活跃的社区和良好的文档

**适用场景：**
- 需要稳定可靠的 IPC 通信
- 需要跨平台支持
- 需要流式数据传输

**示例代码：**
```csharp
// 定义接口
public interface IMyService
{
    Task<string> GetDataAsync(int id);
}

// 服务器端
var server = new JsonRpc(new NamedPipeServerStream("mypipe"), new MyService());
server.StartListening();

// 客户端
var client = new JsonRpc(new NamedPipeClientStream(".", "mypipe", PipeDirection.InOut));
var proxy = client.Attach<IMyService>();
var result = await proxy.GetDataAsync(123);
```

### 2. MagicOnion ⭐⭐⭐⭐

**GitHub:** https://github.com/Cysharp/MagicOnion

**特点：**
- 基于 gRPC，但更简单易用
- 支持 C# 接口定义（无需 .proto 文件）
- 支持流式传输
- 高性能（基于 HTTP/2）
- 支持 Unity（游戏开发）

**适用场景：**
- 需要高性能 IPC
- 需要流式传输
- 需要跨平台支持

**示例代码：**
```csharp
// 定义接口
public interface IMyService : IService<IMyService>
{
    UnaryResult<string> GetDataAsync(int id);
}

// 服务器端
var server = MagicOnionEngine.CreateServer(new MagicOnionOptions
{
    Port = 12345,
    ServiceTypes = new[] { typeof(MyService) }
});
await server.StartAsync();

// 客户端
var client = MagicOnionClient.Create<IMyService>(new Channel("localhost", 12345, ChannelCredentials.Insecure));
var result = await client.GetDataAsync(123);
```

### 3. MessagePipe ⭐⭐⭐⭐

**GitHub:** https://github.com/Cysharp/MessagePipe

**特点：**
- 轻量级消息传递库
- 支持发布-订阅模式
- 支持请求-响应模式
- 支持进程间通信（通过命名管道）
- 高性能，零分配设计

**适用场景：**
- 需要发布-订阅模式
- 需要高性能消息传递
- 需要简单的 IPC 通信

**示例代码：**
```csharp
// 定义消息类型
public class MyMessage { public int Id { get; set; } }

// 发布者
var publisher = provider.GetRequiredService<IPublisher<MyMessage>>();
await publisher.PublishAsync(new MyMessage { Id = 123 });

// 订阅者
var subscriber = provider.GetRequiredService<ISubscriber<MyMessage>>();
await subscriber.SubscribeAsync(async (msg, ct) => {
    Console.WriteLine($"Received: {msg.Id}");
});
```

### 4. gRPC ⭐⭐⭐⭐⭐

**GitHub:** https://github.com/grpc/grpc-dotnet

**特点：**
- 由 Google 开发，行业标准
- 高性能（基于 HTTP/2）
- 跨平台、跨语言
- 强类型接口定义（.proto 文件）
- 支持流式传输
- 丰富的生态系统

**缺点：**
- 需要定义 .proto 文件
- 配置相对复杂

**适用场景：**
- 需要跨语言通信
- 需要高性能
- 需要标准化协议

### 5. WCF (Windows Communication Foundation) ⭐⭐⭐

**特点：**
- Microsoft 官方框架
- 功能强大，支持多种协议
- 支持事务、安全等高级特性

**缺点：**
- 仅支持 Windows
- 配置复杂
- 性能相对较低
- 不再积极开发（推荐使用 gRPC）

## 对比表格

| 库名 | 性能 | 易用性 | 跨平台 | 代码生成 | 流式传输 | 维护状态 | 推荐度 |
|------|------|--------|--------|----------|----------|----------|--------|
| H.Ipc | ⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ✅ | ❌ | ⚠️ | ⭐⭐⭐ |
| StreamJsonRpc | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ❌ | ✅ | ✅ | ⭐⭐⭐⭐⭐ |
| MagicOnion | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ✅ | ✅ | ✅ | ⭐⭐⭐⭐ |
| MessagePipe | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ❌ | ✅ | ✅ | ⭐⭐⭐⭐ |
| gRPC | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ✅ | ✅ | ✅ | ✅ | ⭐⭐⭐⭐⭐ |
| WCF | ⭐⭐⭐ | ⭐⭐ | ❌ | ✅ | ✅ | ⚠️ | ⭐⭐⭐ |

## 针对当前项目的建议

### 推荐方案 1：StreamJsonRpc

**理由：**
- 由 Microsoft 维护，稳定可靠
- 支持命名管道，与当前架构兼容
- 支持强类型接口，无需代码生成器
- 没有 H.Ipc 的类型处理 bug
- 文档完善，社区活跃

**迁移难度：** 中等
- 需要手动定义接口（但更清晰）
- 需要修改服务器和客户端初始化代码
- 接口调用方式类似

### 推荐方案 2：MagicOnion

**理由：**
- 支持 C# 接口定义（无需 .proto）
- 高性能
- 支持流式传输
- 代码生成器更稳定

**迁移难度：** 较高
- 需要引入 gRPC 依赖
- 需要修改传输层（从命名管道改为 gRPC channel）
- 但接口定义方式类似

### 推荐方案 3：继续使用 H.Ipc，但修复类型问题

**理由：**
- 迁移成本最低
- 当前代码已经基于 H.Ipc 实现

**解决方案：**
- 避免使用 `long`、`int?`、`List<T>?` 等类型
- 使用 `string` 替代 `long`
- 使用非可空 `List<T>` 替代 `List<T>?`
- 或者等待 H.Ipc 更新

## 结论

**最佳选择：StreamJsonRpc**
- 稳定可靠，由 Microsoft 维护
- 功能强大，支持流式传输
- 迁移成本适中
- 没有类型处理 bug

**次优选择：继续使用 H.Ipc**
- 如果不想大改，可以规避类型问题
- 使用 `string` 替代复杂类型
- 等待 H.Ipc 更新修复 bug

