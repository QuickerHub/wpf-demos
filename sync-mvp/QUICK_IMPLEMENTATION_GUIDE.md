# 快速实现数据同步方案指南

本文档提供各种同步方案的快速实现代码示例，帮助您快速选择并实现适合的方案。

## 方案 1: HTTP API 同步（推荐用于快速 MVP）

### 特点
- ✅ 实现简单，1天内可完成
- ✅ 使用标准 HTTP，易于调试
- ✅ 不需要特殊协议或框架

### 服务器端实现（ASP.NET Core Minimal API）

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDbContext, AppDbContext>();
var app = builder.Build();

// 存储变更记录
app.MapPost("/api/sync/upload", async (List<ChangeRecord> changes, IDbContext db) =>
{
    foreach (var change in changes)
    {
        change.ReceivedAt = DateTime.UtcNow;
        await db.ChangeRecords.AddAsync(change);
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { count = changes.Count });
});

// 获取变更记录
app.MapGet("/api/sync/download", async (DateTime? since, IDbContext db) =>
{
    var query = db.ChangeRecords.AsQueryable();
    if (since.HasValue)
    {
        query = query.Where(x => x.Timestamp > since.Value);
    }
    var changes = await query.OrderBy(x => x.Timestamp).ToListAsync();
    return Results.Ok(changes);
});

app.Run("http://localhost:5000");
```

### 客户端实现

```csharp
public class HttpSyncService : ISyncService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly IChangeLogService _changeLogService;
    private readonly IItemService _itemService;

    public HttpSyncService(HttpClient httpClient, string baseUrl, 
        IChangeLogService changeLogService, IItemService itemService)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _changeLogService = changeLogService;
        _itemService = itemService;
    }

    public async Task SyncToRemoteAsync()
    {
        var unsyncedChanges = await _changeLogService.GetUnsyncedChangesAsync();
        if (unsyncedChanges.Count == 0) return;

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/sync/upload", unsyncedChanges);
        response.EnsureSuccessStatusCode();

        var changeIds = unsyncedChanges.Select(x => x.Id).ToList();
        await _changeLogService.MarkAsSyncedAsync(changeIds);
    }

    public async Task SyncFromRemoteAsync()
    {
        var lastSyncTime = await _changeLogService.GetLastSyncTimeAsync();
        var response = await _httpClient.GetAsync(
            $"{_baseUrl}/api/sync/download?since={lastSyncTime:O}");
        response.EnsureSuccessStatusCode();

        var remoteChanges = await response.Content.ReadFromJsonAsync<List<ChangeRecord>>();
        if (remoteChanges == null || remoteChanges.Count == 0) return;

        await ApplyChangesAsync(remoteChanges);
    }

    private async Task ApplyChangesAsync(List<ChangeRecord> changes)
    {
        foreach (var change in changes.OrderBy(x => x.Timestamp))
        {
            switch (change.Operation)
            {
                case "INSERT":
                case "UPDATE":
                    var item = JsonSerializer.Deserialize<CommonItem>(change.EntityData);
                    if (item != null)
                        await _itemService.ApplySyncChangeAsync(item);
                    break;
                case "DELETE":
                    await _itemService.ApplySyncDeleteAsync(change.EntityId);
                    break;
            }
        }
    }

    public async Task FullSyncAsync()
    {
        await SyncToRemoteAsync();
        await SyncFromRemoteAsync();
    }
}
```

### 使用方式

```csharp
// 在 DI 容器中注册
services.AddHttpClient<ISyncService, HttpSyncService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
});

// 定时同步（每30秒）
var timer = new Timer(async _ => await syncService.FullSyncAsync(), 
    null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
```

---

## 方案 2: SQLite 文件直接同步（最简单）

### 特点
- ✅ 几乎零代码
- ✅ 适合单用户多设备
- ✅ 可以使用 OneDrive/Dropbox 自动同步

### 实现代码

```csharp
public class FileSyncService
{
    private readonly string _dbPath;
    private readonly string _syncPath;
    private readonly FileSystemWatcher _watcher;

    public FileSyncService(string dbPath, string syncPath)
    {
        _dbPath = dbPath;
        _syncPath = syncPath;
        
        // 监听数据库文件变化
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(dbPath)!)
        {
            Filter = Path.GetFileName(dbPath),
            NotifyFilter = NotifyFilters.LastWrite
        };
        _watcher.Changed += OnDatabaseChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private async void OnDatabaseChanged(object sender, FileSystemEventArgs e)
    {
        // 等待文件写入完成
        await Task.Delay(1000);
        
        // 复制到同步目录
        var syncFile = Path.Combine(_syncPath, "database.db");
        File.Copy(_dbPath, syncFile, overwrite: true);
    }

    public async Task SyncFromRemoteAsync()
    {
        var syncFile = Path.Combine(_syncPath, "database.db");
        if (File.Exists(syncFile))
        {
            // 检查文件是否正在被写入
            var fileInfo = new FileInfo(syncFile);
            var lastWrite = fileInfo.LastWriteTime;
            await Task.Delay(2000); // 等待文件稳定
            
            if (fileInfo.LastWriteTime == lastWrite)
            {
                // 备份当前数据库
                var backupPath = $"{_dbPath}.backup";
                File.Copy(_dbPath, backupPath, overwrite: true);
                
                // 替换数据库文件
                File.Copy(syncFile, _dbPath, overwrite: true);
            }
        }
    }
}
```

### 使用云存储自动同步

**OneDrive 方式**：
1. 将数据库文件放在 OneDrive 同步文件夹
2. OneDrive 自动同步到云端和其他设备
3. 其他设备自动下载最新版本

**Dropbox 方式**：
1. 将数据库文件放在 Dropbox 文件夹
2. Dropbox 自动同步

**注意事项**：
- 避免多设备同时写入
- 使用 SQLite 的 WAL 模式可以减少文件锁冲突
- 建议添加文件锁检测机制

---

## 方案 3: 云存储 SDK 同步（OneDrive/Dropbox）

### OneDrive 实现（Microsoft Graph）

```csharp
// 安装 NuGet: Microsoft.Graph

public class OneDriveSyncService
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _syncFolder = "AppSync";

    public OneDriveSyncService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task SyncToRemoteAsync(List<ChangeRecord> changes)
    {
        var json = JsonSerializer.Serialize(changes);
        var fileName = $"changes_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        
        // 获取或创建同步文件夹
        var folder = await GetOrCreateFolderAsync(_syncFolder);
        
        // 上传文件
        var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await _graphClient.Me.Drive.Root
            .ItemWithPath($"{_syncFolder}/{fileName}")
            .Content
            .Request()
            .PutAsync<DriveItem>(content);
    }

    public async Task<List<ChangeRecord>> SyncFromRemoteAsync()
    {
        var folder = await GetOrCreateFolderAsync(_syncFolder);
        var children = await _graphClient.Me.Drive
            .Items[folder.Id]
            .Children
            .Request()
            .GetAsync();

        var allChanges = new List<ChangeRecord>();
        foreach (var item in children)
        {
            if (item.File != null && item.Name.EndsWith(".json"))
            {
                var stream = await _graphClient.Me.Drive
                    .Items[item.Id]
                    .Content
                    .Request()
                    .GetAsync();
                
                var json = await new StreamReader(stream).ReadToEndAsync();
                var changes = JsonSerializer.Deserialize<List<ChangeRecord>>(json);
                if (changes != null)
                    allChanges.AddRange(changes);
            }
        }
        
        return allChanges;
    }

    private async Task<DriveItem> GetOrCreateFolderAsync(string folderName)
    {
        try
        {
            return await _graphClient.Me.Drive.Root
                .ItemWithPath(folderName)
                .Request()
                .GetAsync();
        }
        catch
        {
            // 文件夹不存在，创建它
            var folder = new DriveItem
            {
                Name = folderName,
                Folder = new Folder()
            };
            return await _graphClient.Me.Drive.Root
                .Children
                .Request()
                .AddAsync(folder);
        }
    }
}
```

### Dropbox 实现

```csharp
// 安装 NuGet: Dropbox.Api

public class DropboxSyncService
{
    private readonly DropboxClient _dropboxClient;
    private readonly string _syncPath = "/AppSync";

    public DropboxSyncService(DropboxClient dropboxClient)
    {
        _dropboxClient = dropboxClient;
    }

    public async Task SyncToRemoteAsync(List<ChangeRecord> changes)
    {
        var json = JsonSerializer.Serialize(changes);
        var fileName = $"{_syncPath}/changes_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        await _dropboxClient.Files.UploadAsync(
            fileName,
            WriteMode.Overwrite.Instance,
            body: content);
    }

    public async Task<List<ChangeRecord>> SyncFromRemoteAsync()
    {
        var listResult = await _dropboxClient.Files.ListFolderAsync(_syncPath);
        var allChanges = new List<ChangeRecord>();

        foreach (var item in listResult.Entries.Where(x => x.IsFile && x.Name.EndsWith(".json")))
        {
            var downloadResult = await _dropboxClient.Files.DownloadAsync(item.PathLower);
            var json = await downloadResult.GetContentAsStringAsync();
            var changes = JsonSerializer.Deserialize<List<ChangeRecord>>(json);
            if (changes != null)
                allChanges.AddRange(changes);
        }

        return allChanges;
    }
}
```

---

## 方案 4: SignalR 实时同步

### 服务器端（ASP.NET Core）

```csharp
// Program.cs
builder.Services.AddSignalR();
var app = builder.Build();

app.MapHub<SyncHub>("/syncHub");
app.Run();

// SyncHub.cs
public class SyncHub : Hub
{
    public async Task BroadcastChange(ChangeRecord change)
    {
        // 广播给所有客户端（除了发送者）
        await Clients.Others.SendAsync("ReceiveChange", change);
    }

    public async Task RequestSync(string clientId)
    {
        // 请求其他客户端发送未同步的变更
        await Clients.Others.SendAsync("SendUnsyncedChanges", clientId);
    }
}
```

### 客户端实现

```csharp
public class SignalRSyncService : ISyncService
{
    private readonly HubConnection _connection;
    private readonly IChangeLogService _changeLogService;
    private readonly IItemService _itemService;

    public SignalRSyncService(string hubUrl, IChangeLogService changeLogService, 
        IItemService itemService)
    {
        _changeLogService = changeLogService;
        _itemService = itemService;
        
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        // 接收远程变更
        _connection.On<ChangeRecord>("ReceiveChange", async change =>
        {
            await ApplyChangeAsync(change);
        });

        // 响应同步请求
        _connection.On<string>("SendUnsyncedChanges", async clientId =>
        {
            var changes = await _changeLogService.GetUnsyncedChangesAsync();
            await _connection.SendAsync("ReceiveChanges", clientId, changes);
        });
    }

    public async Task StartAsync()
    {
        await _connection.StartAsync();
    }

    public async Task SyncToRemoteAsync()
    {
        var changes = await _changeLogService.GetUnsyncedChangesAsync();
        foreach (var change in changes)
        {
            await _connection.SendAsync("BroadcastChange", change);
        }
        
        var changeIds = changes.Select(x => x.Id).ToList();
        await _changeLogService.MarkAsSyncedAsync(changeIds);
    }

    private async Task ApplyChangeAsync(ChangeRecord change)
    {
        // 应用变更逻辑
        // ...
    }
}
```

---

## 方案对比总结

| 方案 | 实现时间 | 代码量 | 服务器需求 | 实时性 | 推荐度 |
|------|---------|--------|-----------|--------|--------|
| HTTP API | 1天 | 中等 | 需要 | 中 | ⭐⭐⭐⭐ |
| SQLite 文件同步 | 0.5天 | 极少 | 不需要 | 低 | ⭐⭐⭐ |
| 云存储 SDK | 1-2天 | 中等 | 不需要 | 中 | ⭐⭐⭐⭐ |
| SignalR | 2-3天 | 较多 | 需要 | 高 | ⭐⭐⭐⭐ |
| 变更日志 + WebDAV | 1-2天 | 中等 | 需要 | 中 | ⭐⭐⭐⭐ |

## 快速开始建议

1. **MVP 验证阶段**：使用 HTTP API 方案（1天可完成）
2. **单用户场景**：使用 SQLite 文件 + 云存储（最简单）
3. **需要实时同步**：使用 SignalR（延迟最低）
4. **生产环境**：升级到 Dotmim.Sync（功能完整）

## 注意事项

1. **冲突处理**：所有方案都需要实现冲突解决策略
2. **数据安全**：生产环境需要加密传输和存储
3. **性能优化**：大数据量时考虑批量同步和压缩
4. **错误处理**：实现重试机制和错误日志
5. **离线支持**：考虑离线队列和网络状态检测
