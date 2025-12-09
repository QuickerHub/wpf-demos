# .NET 数据库同步方案调研

## 方案对比总结

### 1. Dotmim.Sync ⭐⭐⭐⭐⭐

**GitHub**: https://github.com/Mimetis/Dotmim.Sync  
**NuGet**: `Dotmim.Sync.Sqlite`, `Dotmim.Sync.Web.Client`

**优点**：
- ✅ 成熟的框架，功能完整
- ✅ 支持多种数据库（SQLite, SQL Server, MySQL, PostgreSQL）
- ✅ 自动处理冲突检测和解决
- ✅ 支持增量同步，性能优秀
- ✅ 支持 Web API、gRPC、WebDAV 等多种传输方式
- ✅ 有完整的文档和活跃的社区
- ✅ 支持离线模式

**缺点**：
- ❌ 需要额外的服务器或 WebDAV 服务器
- ❌ 配置相对复杂
- ❌ 对于简单的 MVP 可能过于重量级
- ❌ 学习曲线较陡

**适用场景**：
- 生产环境
- 需要完整同步功能
- 多数据库支持需求
- 企业级应用

**示例代码**：
```csharp
var localProvider = new SqliteSyncProvider("local.db");
var remoteOrchestrator = new WebRemoteOrchestrator("https://api.example.com/sync");
var agent = new SyncAgent(localProvider, remoteOrchestrator);
var result = await agent.SynchronizeAsync();
```

---

### 2. PowerSync ⭐⭐⭐⭐

**GitHub**: https://github.com/powersync-ja/powersync-dotnet  
**NuGet**: `PowerSync`

**优点**：
- ✅ 实时同步，延迟低
- ✅ 支持离线模式
- ✅ 专为本地优先（Local-First）应用设计
- ✅ 支持 .NET 8, MAUI, WPF
- ✅ 性能优化（增量查询）
- ✅ 2024年新增 .NET SDK（Alpha）

**缺点**：
- ❌ 需要 PowerSync 服务器（商业服务或自建）
- ❌ .NET SDK 还在 Alpha 阶段
- ❌ 主要面向云服务集成
- ❌ 对于 MVP 可能过于复杂

**适用场景**：
- 需要实时同步的应用
- 本地优先架构
- 云服务集成

---

### 3. LiteSync ⭐⭐⭐

**官网**: https://litesync.io/  
**特点**: SQLite 扩展

**优点**：
- ✅ 无缝集成 SQLite
- ✅ 支持多平台（Windows, Linux, Android, iOS, macOS）
- ✅ 离线支持
- ✅ 配置简单（URI 参数）

**缺点**：
- ❌ 需要 SQLite 扩展（不是纯 .NET 库）
- ❌ 文档相对较少
- ❌ 社区支持不如 Dotmim.Sync

**适用场景**：
- 跨平台应用
- 需要 SQLite 原生集成

---

### 4. 变更日志 + WebDAV（自定义方案）⭐⭐⭐⭐

**优点**：
- ✅ 实现简单，快速开发
- ✅ 不需要额外的服务器（使用 WebDAV）
- ✅ 易于理解和调试
- ✅ 可以逐步演进到更复杂的方案
- ✅ 完全控制同步逻辑
- ✅ 适合 MVP 验证

**缺点**：
- ❌ 需要自己实现冲突解决
- ❌ 需要手动处理变更日志
- ❌ 需要实现 WebDAV 客户端
- ❌ 生产环境需要更多测试

**适用场景**：
- MVP、原型验证
- 学习同步机制
- 需要完全控制同步逻辑

**实现要点**：
```csharp
// 1. 记录变更
await _changeLogService.LogChangeAsync(new ChangeRecord
{
    Operation = "UPDATE",
    EntityId = item.Id,
    EntityData = JsonSerializer.Serialize(item)
});

// 2. 上传变更到 WebDAV
await UploadChangesToWebDavAsync(changes);

// 3. 下载并应用远程变更
var remoteChanges = await DownloadChangesFromWebDavAsync();
await ApplyChangesAsync(remoteChanges);
```

---

### 5. SignalR + SQLite ⭐⭐⭐⭐

**NuGet**: `Microsoft.AspNetCore.SignalR.Client`

**优点**：
- ✅ 实时同步，延迟最低
- ✅ 支持双向通信
- ✅ 有成熟的框架支持
- ✅ 支持离线队列

**缺点**：
- ❌ 需要 SignalR 服务器
- ❌ 需要处理连接状态
- ❌ 需要实现同步协议
- ❌ 对于 MVP 可能过于复杂

**适用场景**：
- 需要实时同步的应用
- 已有 ASP.NET Core 服务器
- 聊天、协作类应用

---

### 6. Entity Framework Core Change Tracking ⭐⭐⭐

**NuGet**: `Microsoft.EntityFrameworkCore.Sqlite`

**优点**：
- ✅ 内置变更跟踪
- ✅ 成熟的 ORM
- ✅ 自动生成 SQL

**缺点**：
- ❌ 需要自己实现同步逻辑
- ❌ 变更跟踪主要用于数据库操作，不是同步
- ❌ 需要额外的同步层

**适用场景**：
- 已有 EF Core 项目
- 需要 ORM 功能
- 作为同步的基础层

---

### 7. SQLite 数据库文件直接同步 ⭐⭐⭐

**特点**: 直接同步整个 SQLite 数据库文件

**优点**：
- ✅ 实现最简单，几乎零代码
- ✅ 不需要额外的框架
- ✅ 适合小数据量（< 100MB）
- ✅ 可以使用云存储（OneDrive、Dropbox、Google Drive）

**缺点**：
- ❌ 全量同步，性能差
- ❌ 冲突处理困难（文件锁）
- ❌ 不适合多客户端同时写入
- ❌ 数据量大时效率低

**适用场景**：
- 单用户多设备同步
- 数据量小（< 10MB）
- 不需要实时同步
- 概念验证

**实现要点**：
```csharp
// 1. 使用 FileSystemWatcher 监听数据库文件变化
var watcher = new FileSystemWatcher(dbDirectory);
watcher.Changed += async (s, e) => {
    if (e.Name == "database.db") {
        await SyncDatabaseFileAsync();
    }
};

// 2. 上传到云存储或文件服务器
await UploadFileAsync("database.db", remotePath);

// 3. 下载并替换本地文件
await DownloadFileAsync(remotePath, "database.db");
```

**推荐工具**：
- OneDrive / Dropbox / Google Drive（自动同步）
- rclone（命令行工具）
- WebDAV（文件服务器）

---

### 8. 基于 HTTP API 的简单同步 ⭐⭐⭐⭐

**特点**: 使用简单的 REST API 进行数据同步

**优点**：
- ✅ 实现简单，使用标准 HTTP
- ✅ 易于调试（可以用 Postman 测试）
- ✅ 不需要特殊协议
- ✅ 可以使用现有的 Web 服务器

**缺点**：
- ❌ 需要实现 API 服务器
- ❌ 需要自己处理冲突
- ❌ 轮询方式延迟较高

**适用场景**：
- 已有 Web 服务器
- 需要简单的同步功能
- 客户端数量不多

**实现要点**：
```csharp
// 客户端：上传变更
public async Task SyncToServerAsync()
{
    var changes = await GetUnsyncedChangesAsync();
    var response = await _httpClient.PostAsJsonAsync("/api/sync/upload", changes);
    response.EnsureSuccessStatusCode();
}

// 客户端：下载变更
public async Task SyncFromServerAsync()
{
    var lastSyncTime = GetLastSyncTime();
    var response = await _httpClient.GetAsync($"/api/sync/download?since={lastSyncTime}");
    var changes = await response.Content.ReadFromJsonAsync<List<ChangeRecord>>();
    await ApplyChangesAsync(changes);
}

// 服务器端：ASP.NET Core Minimal API
app.MapPost("/api/sync/upload", async (List<ChangeRecord> changes) => {
    await SaveChangesAsync(changes);
    return Results.Ok();
});

app.MapGet("/api/sync/download", async (DateTime since) => {
    var changes = await GetChangesSinceAsync(since);
    return Results.Ok(changes);
});
```

---

### 9. 基于云存储的文件同步 ⭐⭐⭐⭐

**特点**: 使用 OneDrive、Dropbox、Google Drive 等云存储同步变更日志文件

**优点**：
- ✅ 不需要自己的服务器
- ✅ 利用云存储的同步能力
- ✅ 免费额度通常足够
- ✅ 跨平台支持好

**缺点**：
- ❌ 需要集成云存储 SDK
- ❌ 同步延迟取决于云存储
- ❌ 需要处理文件冲突
- ❌ 可能有 API 限制

**适用场景**：
- 个人应用或小团队
- 不需要实时同步
- 希望利用现有云存储

**推荐 SDK**：
- **OneDrive**: `Microsoft.Graph` (Microsoft Graph API)
- **Dropbox**: `Dropbox.Api`
- **Google Drive**: `Google.Apis.Drive.v3`

**实现要点**：
```csharp
// 使用 Microsoft Graph (OneDrive)
var graphClient = new GraphServiceClient(authProvider);
var driveItem = new DriveItem
{
    Name = $"changes_{DateTime.UtcNow:yyyyMMddHHmmss}.json",
    File = new File()
};
var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
await graphClient.Me.Drive.Root.Children.Request().AddAsync(driveItem, content);
```

---

### 10. 基于消息队列的同步 ⭐⭐⭐

**特点**: 使用消息队列（RabbitMQ、Redis、Azure Service Bus）进行变更推送

**优点**：
- ✅ 实时性好
- ✅ 支持多客户端
- ✅ 有成熟的框架支持
- ✅ 支持离线队列

**缺点**：
- ❌ 需要消息队列服务器
- ❌ 配置相对复杂
- ❌ 需要处理消息持久化
- ❌ 成本较高（云服务）

**适用场景**：
- 需要实时同步
- 已有消息队列基础设施
- 企业级应用

**推荐方案**：
- **RabbitMQ**: `RabbitMQ.Client`（开源，功能完整）
- **Redis Pub/Sub**: `StackExchange.Redis`（轻量级）
- **Azure Service Bus**: `Azure.Messaging.ServiceBus`（云服务）

**实现要点**：
```csharp
// 发布变更
using var channel = _connection.CreateModel();
channel.QueueDeclare("sync_changes", durable: true, exclusive: false);
var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(change));
channel.BasicPublish(exchange: "", routingKey: "sync_changes", body: body);

// 订阅变更
var consumer = new EventingBasicConsumer(channel);
consumer.Received += async (model, ea) => {
    var body = ea.Body.ToArray();
    var change = JsonSerializer.Deserialize<ChangeRecord>(body);
    await ApplyChangeAsync(change);
};
channel.BasicConsume("sync_changes", autoAck: true, consumer);
```

---

### 11. 基于 Git 的同步方案 ⭐⭐⭐

**特点**: 将数据库变更作为 JSON 文件提交到 Git 仓库

**优点**：
- ✅ 利用 Git 的版本控制能力
- ✅ 自动处理冲突（Git merge）
- ✅ 有完整的历史记录
- ✅ 可以使用 GitHub/GitLab 等免费服务

**缺点**：
- ❌ 需要 Git 知识
- ❌ 不适合二进制数据
- ❌ 同步延迟较高
- ❌ 需要处理 Git 操作

**适用场景**：
- 配置数据同步
- 需要版本历史
- 开发者工具

**实现要点**：
```csharp
// 使用 LibGit2Sharp
using var repo = new Repository(repoPath);
var changes = await GetUnsyncedChangesAsync();
var json = JsonSerializer.Serialize(changes);
File.WriteAllText("changes.json", json);

Commands.Stage(repo, "changes.json");
var signature = new Signature("App", "app@example.com", DateTimeOffset.Now);
repo.Commit("Sync changes", signature, signature);

repo.Network.Push(repo.Head);
```

---

### 12. SQLite WAL + 文件同步 ⭐⭐⭐

**特点**: 利用 SQLite WAL（Write-Ahead Logging）模式，同步 WAL 文件

**优点**：
- ✅ 增量同步（只同步 WAL 文件）
- ✅ SQLite 原生支持
- ✅ 性能较好

**缺点**：
- ❌ 需要理解 SQLite WAL 机制
- ❌ 需要处理 WAL 文件合并
- ❌ 多客户端写入仍有冲突风险

**适用场景**：
- 单写多读场景
- 需要增量同步
- 熟悉 SQLite 内部机制

**实现要点**：
```csharp
// 启用 WAL 模式
db.Execute("PRAGMA journal_mode=WAL;");

// 同步 WAL 文件
var walFile = Path.Combine(dbDirectory, "database.db-wal");
if (File.Exists(walFile)) {
    await SyncFileAsync(walFile, remoteWalPath);
}

// 定期 checkpoint（合并 WAL）
db.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
```

---

## MVP 推荐方案

### 第一阶段：变更日志 + WebDAV（当前实现）

**理由**：
1. 快速实现，可以验证同步概念
2. 不需要额外的服务器
3. 易于理解和调试
4. 学习价值高

### 第二阶段：升级到 Dotmim.Sync

**时机**：
- MVP 验证成功后
- 需要更完善的冲突解决
- 需要支持更多数据库
- 需要更好的性能

---

## 快速选择指南

### 根据需求选择方案

| 需求场景 | 推荐方案 | 理由 |
|---------|---------|------|
| **最快实现 MVP** | 变更日志 + WebDAV（当前）或 HTTP API | 代码量少，易于理解 |
| **单用户多设备** | SQLite 文件直接同步 + 云存储 | 最简单，零代码 |
| **需要实时同步** | SignalR 或 消息队列 | 延迟最低 |
| **生产环境** | Dotmim.Sync | 功能完整，稳定可靠 |
| **已有 Web 服务器** | HTTP API | 利用现有基础设施 |
| **个人/小团队** | 云存储文件同步 | 免费，无需服务器 |
| **需要版本历史** | Git 同步 | 自动版本控制 |
| **企业级应用** | Dotmim.Sync 或 消息队列 | 可扩展，功能完整 |

### 根据数据量选择

| 数据量 | 推荐方案 |
|-------|---------|
| < 10MB | SQLite 文件直接同步 |
| 10MB - 100MB | 变更日志 + WebDAV/HTTP API |
| 100MB - 1GB | Dotmim.Sync |
| > 1GB | Dotmim.Sync + 增量同步 |

### 根据客户端数量选择

| 客户端数 | 推荐方案 |
|---------|---------|
| 1-2 个 | SQLite 文件同步 |
| 3-10 个 | 变更日志 + WebDAV/HTTP API |
| 10-100 个 | Dotmim.Sync 或 SignalR |
| > 100 个 | Dotmim.Sync + 消息队列 |

## 性能对比

| 方案 | 同步延迟 | 性能 | 复杂度 | 适用规模 | 实现时间 |
|------|---------|------|--------|---------|---------|
| Dotmim.Sync | 低 | ⭐⭐⭐⭐⭐ | 中 | 大 | 2-3天 |
| PowerSync | 极低 | ⭐⭐⭐⭐⭐ | 高 | 大 | 3-5天 |
| LiteSync | 低 | ⭐⭐⭐⭐ | 中 | 中 | 2-3天 |
| 变更日志 + WebDAV | 中 | ⭐⭐⭐ | 低 | 小-中 | 1-2天 |
| SignalR | 极低 | ⭐⭐⭐⭐ | 中 | 中-大 | 2-3天 |
| HTTP API | 中 | ⭐⭐⭐ | 低 | 小-中 | 1天 |
| SQLite 文件同步 | 高 | ⭐⭐ | 极低 | 小 | 0.5天 |
| 云存储同步 | 中-高 | ⭐⭐⭐ | 低 | 小-中 | 1-2天 |
| 消息队列 | 极低 | ⭐⭐⭐⭐ | 中 | 中-大 | 2-3天 |
| Git 同步 | 高 | ⭐⭐ | 中 | 小 | 1-2天 |
| SQLite WAL | 中 | ⭐⭐⭐ | 中 | 小-中 | 1-2天 |

---

## 总结

对于 MVP，推荐使用**变更日志 + WebDAV**方案：
- ✅ 快速实现
- ✅ 易于理解
- ✅ 可以验证概念
- ✅ 后续可以升级到 Dotmim.Sync

对于生产环境，推荐使用**Dotmim.Sync**：
- ✅ 功能完整
- ✅ 性能优秀
- ✅ 社区支持好
- ✅ 文档完善
