# WebDAV 数据同步方案

## 方案概述

本方案基于 WebDAV 协议实现 SQLite 数据库的增量同步，特别适用于**剪贴板、收藏等隐私数据**的跨设备同步。

### 核心优势

- ✅ **隐私保护**：数据存储在用户自己的 WebDAV 服务器，完全由用户控制
- ✅ **无需第三方服务**：用户配置自己的 WebDAV 服务器（NAS、云存储等）
- ✅ **增量同步**：只同步变更记录，效率高
- ✅ **离线支持**：变更记录本地缓存，网络恢复后自动同步
- ✅ **多设备支持**：支持多客户端同时使用
- ✅ **标准协议**：WebDAV 是标准 HTTP 扩展，兼容性好

### 适用场景

- 剪贴板历史同步
- 收藏夹/书签同步
- 个人笔记同步
- 配置数据同步
- 其他需要隐私保护的敏感数据

---

## 架构设计

### 核心思想

**WebDAV 通过单一文件夹和通用同步 item 管理所有同步**：
- **单一文件夹**：`sync/` 文件夹统一管理所有同步数据
- **通用同步 item**：所有需要同步的数据都用 `SyncItem` 类包装（本质上就是同步一段 JSON 内容）
- **文件 = 同步 item**：每个同步 item 存储为一个独立的 JSON 文件

### 整体架构

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  设备 A     │         │  设备 B     │         │  设备 C     │
│             │         │             │         │             │
│ SQLite DB   │         │ SQLite DB   │         │ SQLite DB   │
│ ChangeLog   │         │ ChangeLog   │         │ ChangeLog   │
│ (本地)      │         │ (本地)      │         │ (本地)      │
└──────┬──────┘         └──────┬──────┘         └──────┬──────┘
       │                       │                       │
       │      WebDAV 协议      │      WebDAV 协议      │
       │      (文件操作)       │      (文件操作)       │
       └───────────┬───────────┴───────────┬───────────┘
                   │                        │
                   └──────────┬─────────────┘
                              │
                    ┌─────────▼─────────┐
                    │   WebDAV Server   │
                    │                   │
                    │  sync/            │  ← 单一同步文件夹
                    │    ├─ {id1}.json  │  ← 同步 item（文件）
                    │    ├─ {id2}.json  │
                    │    ├─ {id3}.json  │
                    │    └─ {id4}.json  │
                    └───────────────────┘
```

### 文件结构

**单一文件夹**：
- `sync/` - 统一管理所有同步数据

**文件 = 同步 item**：
每个同步 item 存储为一个独立的 JSON 文件，文件名使用 item 的 ID：

```
sync/
  ├─ 550e8400-e29b-41d4-a716-446655440000.json  ← 同步 item 1（剪贴板数据）
  ├─ 6ba7b810-9dad-11d1-80b4-00c04fd430c8.json  ← 同步 item 2（收藏数据）
  ├─ 6ba7b811-9dad-11d1-80b4-00c04fd430c8.json  ← 同步 item 3（笔记数据）
  └─ 6ba7b812-9dad-11d1-80b4-00c04fd430c8.json  ← 同步 item 4（其他数据）
```

### 同步流程（基于单一文件夹和文件操作）

1. **本地变更记录**
   - 用户操作（增删改）时，记录到本地 `ChangeRecord` 表
   - 将变更数据包装为 `SyncItem`（包含表名、实体ID、JSON 数据等）
   - 每个变更包含：操作类型、表名、实体ID、JSON 数据、时间戳、序列号

2. **同步到 WebDAV（模拟 INSERT 操作）**
   - 遍历本地未同步的变更记录
   - 对于每个变更记录：
     - 包装为 `SyncItem`
     - 创建文件：`PUT /sync/{SyncItemId}.json`
     - 写入 `SyncItem` 的 JSON 内容
   - 标记本地变更记录为已同步

3. **从 WebDAV 同步（模拟 SELECT 操作）**
   - 列出 `sync/` 文件夹中的所有文件：`PROPFIND /sync/`
   - 获取本地已同步的最大序列号
   - 对于每个同步 item 文件：
     - 读取文件：`GET /sync/{SyncItemId}.json`
     - 反序列化为 `SyncItem`
     - 如果序列号 > 本地最大序列号，则应用变更
   - 按时间戳和序列号排序后应用变更

4. **冲突解决**
   - 使用版本号（Version）机制
   - Last Write Wins 策略
   - 文件级别的乐观并发控制（通过 ETag）

---

## 数据模型

### SyncItem（通用同步 Item）

**核心设计**：所有需要同步的数据都用 `SyncItem` 类包装，本质上就是同步一段 JSON 内容。

```csharp
/// <summary>
/// 通用同步 Item，用于包装任意需要同步的数据
/// </summary>
public class SyncItem
{
    /// <summary>
    /// 同步 Item 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 变更发生的时间戳（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 操作类型：INSERT, UPDATE, DELETE
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// 实体表名（如：ClipboardItem, Favorite, Note）
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// 数据内容（JSON 字符串）- 任意数据的 JSON 序列化
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID（设备标识）
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 序列号（用于排序和增量同步）
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// 版本号（用于冲突检测）
    /// </summary>
    public int Version { get; set; } = 1;
}
```

**使用示例**：

```csharp
// 包装剪贴板数据
var clipboardItem = new ClipboardItem 
{ 
    Id = "clip-1", 
    Content = "Hello World",
    CreatedAt = DateTime.UtcNow 
};

var syncItem = new SyncItem
{
    Id = Guid.NewGuid().ToString(),
    Timestamp = DateTime.UtcNow,
    Operation = "INSERT",
    TableName = "ClipboardItem",
    EntityId = clipboardItem.Id,
    Data = JsonSerializer.Serialize(clipboardItem), // 包装为 JSON
    ClientId = "DeviceA",
    SequenceNumber = 1,
    Version = 1
};

// 包装收藏数据
var favoriteItem = new Favorite 
{ 
    Id = "fav-1", 
    Title = "Example",
    Url = "https://example.com"
};

var syncItem2 = new SyncItem
{
    Id = Guid.NewGuid().ToString(),
    Timestamp = DateTime.UtcNow,
    Operation = "INSERT",
    TableName = "Favorite",
    EntityId = favoriteItem.Id,
    Data = JsonSerializer.Serialize(favoriteItem), // 包装为 JSON
    ClientId = "DeviceA",
    SequenceNumber = 2,
    Version = 1
};
```

### ChangeRecord（本地变更记录）

```csharp
/// <summary>
/// 本地变更记录（用于跟踪同步状态）
/// </summary>
public class ChangeRecord
{
    /// <summary>
    /// 变更记录唯一标识（对应 SyncItem.Id）
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 变更发生的时间戳（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 操作类型：INSERT, UPDATE, DELETE
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// 实体表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// 序列号（用于排序）
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// 是否已同步到 WebDAV
    /// </summary>
    public bool IsSynced { get; set; } = false;
}
```

### 实体表要求

所有需要同步的实体表必须包含以下字段：

```csharp
public class BaseSyncEntity
{
    /// <summary>
    /// 实体唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 版本号（用于冲突检测）
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间（UTC）
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 客户端ID（最后修改的设备）
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}
```

---

## 核心实现

### 1. 变更日志服务（ChangeLogService）

```csharp
public interface IChangeLogService
{
    /// <summary>
    /// 记录变更
    /// </summary>
    Task LogChangeAsync(ChangeRecord change);

    /// <summary>
    /// 获取未同步的变更
    /// </summary>
    Task<List<ChangeRecord>> GetUnsyncedChangesAsync();

    /// <summary>
    /// 标记变更已同步
    /// </summary>
    Task MarkAsSyncedAsync(List<string> changeIds);

    /// <summary>
    /// 获取本地已同步的最大序列号（用于增量同步）
    /// </summary>
    Task<long?> GetMaxSyncedSequenceNumberAsync();

    /// <summary>
    /// 获取指定时间后的变更
    /// </summary>
    Task<List<ChangeRecord>> GetChangesSinceAsync(DateTime timestamp);

    /// <summary>
    /// 清理已同步的旧记录（可选，用于数据库维护）
    /// </summary>
    Task CleanupOldRecordsAsync(TimeSpan retentionPeriod);
}
```

**实现要点**：
- 使用 SQLite 存储变更记录
- 序列号自动递增，确保顺序
- 支持批量标记已同步
- 提供最大序列号查询，用于增量同步
- 可选的清理机制，避免数据库过大

### 2. WebDAV 同步服务（WebDavSyncService）

```csharp
public interface ISyncService
{
    /// <summary>
    /// 同步到远程（上传本地变更）
    /// </summary>
    Task SyncToRemoteAsync();

    /// <summary>
    /// 从远程同步（下载并应用远程变更）
    /// </summary>
    Task SyncFromRemoteAsync();

    /// <summary>
    /// 完整同步（上传 + 下载）
    /// </summary>
    Task FullSyncAsync();

    /// <summary>
    /// 同步完成事件
    /// </summary>
    event EventHandler<SyncResult>? SyncCompleted;
}
```

**实现要点**：
- 使用 HttpClient 实现 WebDAV 操作
- 支持 PUT（上传）、GET（下载）、PROPFIND（列表）
- 错误处理和重试机制
- 支持 HTTPS 和基本认证

### 3. WebDAV 单一文件夹操作实现

#### 上传同步 Item（模拟 INSERT）

```csharp
/// <summary>
/// 将本地未同步的变更记录上传到 WebDAV
/// </summary>
public async Task SyncToRemoteAsync()
{
    var unsyncedChanges = await _changeLogService.GetUnsyncedChangesAsync();
    if (unsyncedChanges.Count == 0)
        return;

    // 确保 sync 文件夹存在
    await EnsureSyncFolderExistsAsync();
    
    // 获取全局最大序列号
    var maxSeq = await GetGlobalMaxSequenceNumberAsync();
    
    // 上传每个变更记录为 SyncItem
    foreach (var change in unsyncedChanges)
    {
        try
        {
            // 分配序列号
            maxSeq++;
            
            // 从本地数据库获取完整的实体数据
            var entityData = await GetEntityDataAsync(change.TableName, change.EntityId);
            
            // 包装为 SyncItem
            var syncItem = new SyncItem
            {
                Id = change.Id,
                Timestamp = change.Timestamp,
                Operation = change.Operation,
                TableName = change.TableName,
                EntityId = change.EntityId,
                Data = entityData, // JSON 字符串
                ClientId = _clientId,
                SequenceNumber = maxSeq,
                Version = 1
            };
            
            // 上传文件：PUT /sync/{SyncItemId}.json
            var filePath = $"{_webDavUrl}/sync/{syncItem.Id}.json";
            var json = JsonSerializer.Serialize(syncItem, new JsonSerializerOptions 
            { 
                WriteIndented = false  // 压缩格式
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(filePath, content);
            
            if (response.IsSuccessStatusCode)
            {
                // 标记为已同步
                await _changeLogService.MarkAsSyncedAsync(new List<string> { change.Id });
                
                _logger.LogDebug("Uploaded sync item {SyncItemId} for {TableName}", 
                    syncItem.Id, change.TableName);
            }
            else
            {
                _logger.LogWarning("Failed to upload sync item {SyncItemId}: {StatusCode}", 
                    syncItem.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading sync item {ChangeId}", change.Id);
            // 继续处理下一个变更
        }
    }
    
    _logger.LogInformation("Synced {Count} changes to remote", unsyncedChanges.Count);
}

/// <summary>
/// 确保 sync 文件夹存在
/// </summary>
private async Task EnsureSyncFolderExistsAsync()
{
    var folderPath = $"{_webDavUrl}/sync/";
    
    try
    {
        // 尝试创建文件夹（如果不存在）
        var request = new HttpRequestMessage(new HttpMethod("MKCOL"), folderPath);
        var response = await _httpClient.SendAsync(request);
        
        // 201 Created 表示创建成功，405 Method Not Allowed 表示已存在
        if (response.StatusCode != HttpStatusCode.Created && 
            response.StatusCode != HttpStatusCode.MethodNotAllowed)
        {
            response.EnsureSuccessStatusCode();
        }
    }
    catch (HttpRequestException ex)
    {
        _logger.LogWarning(ex, "Failed to create sync folder, may already exist");
    }
}

/// <summary>
/// 获取全局最大序列号（从 sync 文件夹中的所有文件）
/// </summary>
private async Task<long> GetGlobalMaxSequenceNumberAsync()
{
    var syncItems = await ReadAllSyncItemsAsync();
    if (syncItems.Any())
    {
        return syncItems.Max(x => x.SequenceNumber);
    }
    return 0;
}

/// <summary>
/// 从本地数据库获取实体数据（JSON 字符串）
/// </summary>
private async Task<string> GetEntityDataAsync(string tableName, string entityId)
{
    // 根据表名和实体ID从本地数据库获取数据
    // 返回 JSON 序列化的字符串
    // 这里需要根据实际的数据库访问方式实现
    return await _itemService.GetEntityDataAsJsonAsync(tableName, entityId);
}
```

#### 下载同步 Item（模拟 SELECT）

```csharp
/// <summary>
/// 从 WebDAV 下载同步 Item 并应用到本地
/// </summary>
public async Task SyncFromRemoteAsync()
{
    try
    {
        // 1. 读取 sync 文件夹中的所有同步 Item
        var remoteSyncItems = await ReadAllSyncItemsAsync();
        
        if (remoteSyncItems.Count == 0)
        {
            _logger.LogInformation("No sync items found on WebDAV");
            return;
        }
        
        // 2. 获取本地已同步的最大序列号
        var localMaxSeq = await _changeLogService.GetMaxSyncedSequenceNumberAsync() ?? 0;
        
        // 3. 筛选未同步的变更（序列号 > 本地最大序列号）
        var unsyncedItems = remoteSyncItems
            .Where(x => x.SequenceNumber > localMaxSeq)
            .OrderBy(x => x.SequenceNumber)
            .ToList();
        
        if (unsyncedItems.Count == 0)
        {
            _logger.LogInformation("All remote changes are already synced");
            return;
        }
        
        // 4. 应用变更到本地数据库
        await ApplySyncItemsAsync(unsyncedItems);
        
        _logger.LogInformation("Synced {Count} changes from remote", unsyncedItems.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to sync from remote");
        throw;
    }
}

/// <summary>
/// 读取 sync 文件夹中的所有同步 Item
/// </summary>
private async Task<List<SyncItem>> ReadAllSyncItemsAsync()
{
    var syncItems = new List<SyncItem>();
    var syncFolderPath = $"{_webDavUrl}/sync/";
    
    try
    {
        // 列出 sync 文件夹中的所有文件
        var files = await ListFilesInFolderAsync(syncFolderPath);
        
        // 并行读取文件（限制并发数）
        var semaphore = new SemaphoreSlim(10); // 最多10个并发请求
        var tasks = files
            .Where(f => f.EndsWith(".json"))
            .Select(async fileName =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var filePath = $"{syncFolderPath}{fileName}";
                    return await ReadSyncItemFileAsync(filePath);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        
        var results = await Task.WhenAll(tasks);
        syncItems.AddRange(results.Where(x => x != null)!);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to read sync items from WebDAV");
    }
    
    return syncItems;
}

/// <summary>
/// 列出文件夹中的所有文件
/// </summary>
private async Task<List<string>> ListFilesInFolderAsync(string folderPath)
{
    try
    {
        var request = new HttpRequestMessage(HttpMethod.PropFind, folderPath);
        request.Headers.Add("Depth", "1");
        
        var content = new StringContent(
            @"<?xml version=""1.0"" encoding=""utf-8""?>
            <D:propfind xmlns:D=""DAV:"">
                <D:prop>
                    <D:resourcetype/>
                </D:prop>
            </D:propfind>",
            Encoding.UTF8,
            "application/xml");
        
        request.Content = content;
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var xml = await response.Content.ReadAsStringAsync();
        return ParsePropFindResponseForFiles(xml);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to list files in folder {FolderPath}", folderPath);
        return new List<string>();
    }
}

/// <summary>
/// 读取单个同步 Item 文件
/// </summary>
private async Task<SyncItem?> ReadSyncItemFileAsync(string filePath)
{
    try
    {
        var response = await _httpClient.GetAsync(filePath);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var syncItem = JsonSerializer.Deserialize<SyncItem>(json);
        
        return syncItem;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to read sync item file {FilePath}", filePath);
        return null;
    }
}

/// <summary>
/// 应用同步 Item 到本地数据库
/// </summary>
private async Task ApplySyncItemsAsync(List<SyncItem> syncItems)
{
    foreach (var syncItem in syncItems)
    {
        try
        {
            switch (syncItem.Operation)
            {
                case "INSERT":
                case "UPDATE":
                    // 反序列化 Data 字段为对应的实体类型
                    await ApplyEntityDataAsync(syncItem.TableName, syncItem.EntityId, syncItem.Data);
                    break;
                    
                case "DELETE":
                    await DeleteEntityAsync(syncItem.TableName, syncItem.EntityId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply sync item {SyncItemId}", syncItem.Id);
            // 继续处理下一个
        }
    }
}

/// <summary>
/// 应用实体数据到本地数据库
/// </summary>
private async Task ApplyEntityDataAsync(string tableName, string entityId, string jsonData)
{
    // 根据表名反序列化为对应的实体类型，然后保存到数据库
    await _itemService.ApplyEntityDataAsync(tableName, entityId, jsonData);
}

/// <summary>
/// 删除实体
/// </summary>
private async Task DeleteEntityAsync(string tableName, string entityId)
{
    await _itemService.DeleteEntityAsync(tableName, entityId);
}

/// <summary>
/// 解析 PROPFIND 响应，提取文件列表
/// </summary>
private List<string> ParsePropFindResponseForFiles(string xml)
{
    var files = new List<string>();
    // TODO: 实现 XML 解析逻辑
    // 使用 XDocument 或 XmlDocument 解析 XML
    // 提取 resourcetype 不为 collection 的项（即文件）
    return files;
}
```

---

## 冲突解决策略

### 1. Last Write Wins（最后写入获胜）

```csharp
private async Task ApplyChangeAsync(ChangeRecord change)
{
    var entity = JsonSerializer.Deserialize<BaseSyncEntity>(change.EntityData);
    if (entity == null) return;
    
    var existing = await GetEntityByIdAsync(change.TableName, change.EntityId);
    
    switch (change.Operation)
    {
        case "INSERT":
            if (existing == null)
            {
                await InsertEntityAsync(change.TableName, entity);
            }
            break;
            
        case "UPDATE":
            if (existing == null)
            {
                await InsertEntityAsync(change.TableName, entity);
            }
            else if (entity.Version > existing.Version)
            {
                // 远程版本更新，应用远程变更
                await UpdateEntityAsync(change.TableName, entity);
            }
            // 否则保留本地版本（本地版本更新）
            break;
            
        case "DELETE":
            if (existing != null && entity.Version >= existing.Version)
            {
                await DeleteEntityAsync(change.TableName, change.EntityId);
            }
            break;
    }
}
```

### 2. 手动合并策略（可选）

对于复杂冲突，可以记录冲突并提示用户：

```csharp
public class ConflictRecord
{
    public string Id { get; set; }
    public string TableName { get; set; }
    public string EntityId { get; set; }
    public string LocalData { get; set; }
    public string RemoteData { get; set; }
    public DateTime ConflictTime { get; set; }
    public ConflictResolution Resolution { get; set; }
}

public enum ConflictResolution
{
    Pending,      // 待处理
    UseLocal,     // 使用本地
    UseRemote,    // 使用远程
    Merged        // 已合并
}
```

---

## 安全性考虑

### 1. 传输加密

- **必须使用 HTTPS**：WebDAV 服务器必须支持 HTTPS
- **证书验证**：验证服务器证书有效性
- **TLS 1.2+**：使用现代 TLS 版本

```csharp
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        // 生产环境应验证证书
        if (errors == SslPolicyErrors.None)
            return true;
        
        // 开发环境可以放宽（不推荐）
        return false;
    }
};

var httpClient = new HttpClient(handler);
```

### 2. 认证机制

支持多种认证方式：

```csharp
// 基本认证（用户名密码）
var credentials = Convert.ToBase64String(
    Encoding.ASCII.GetBytes($"{username}:{password}"));
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Basic", credentials);

// Bearer Token（如果 WebDAV 服务器支持）
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

### 3. 数据加密（可选）

对于极度敏感的数据，可以在应用层加密：

```csharp
public class EncryptedChangeRecord
{
    public string Id { get; set; }
    public string EncryptedData { get; set; }  // AES 加密后的数据
    public string Iv { get; set; }              // 初始化向量
}

// 上传前加密
var encrypted = EncryptData(changeRecord, encryptionKey);

// 下载后解密
var decrypted = DecryptData(encrypted, encryptionKey);
```

---

## 配置管理

### WebDAV 配置

```csharp
public class WebDavSyncConfig
{
    /// <summary>
    /// WebDAV 服务器 URL（如：https://webdav.example.com）
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码（应加密存储）
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 同步间隔（秒）
    /// </summary>
    public int SyncIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 是否自动同步
    /// </summary>
    public bool AutoSync { get; set; } = true;

    /// <summary>
    /// 是否启用加密
    /// </summary>
    public bool EnableEncryption { get; set; } = false;

    /// <summary>
    /// 加密密钥（如果启用加密）
    /// </summary>
    public string? EncryptionKey { get; set; }
}
```

### 配置存储

```csharp
// 使用 Windows DPAPI 加密存储密码
public class SecureConfigStorage
{
    public void SaveConfig(WebDavSyncConfig config)
    {
        // 加密密码
        var encryptedPassword = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(config.Password),
            null,
            DataProtectionScope.CurrentUser);
        
        var configJson = JsonSerializer.Serialize(new
        {
            config.ServerUrl,
            config.Username,
            Password = Convert.ToBase64String(encryptedPassword),
            config.SyncIntervalSeconds,
            config.AutoSync,
            config.EnableEncryption
        });
        
        var configPath = GetConfigPath();
        File.WriteAllText(configPath, configJson);
    }
    
    public WebDavSyncConfig LoadConfig()
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
            return new WebDavSyncConfig();
        
        var json = File.ReadAllText(configPath);
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        
        // 解密密码
        var encryptedPassword = Convert.FromBase64String(
            data.GetProperty("Password").GetString()!);
        var password = Encoding.UTF8.GetString(
            ProtectedData.Unprotect(encryptedPassword, null, 
                DataProtectionScope.CurrentUser));
        
        return new WebDavSyncConfig
        {
            ServerUrl = data.GetProperty("ServerUrl").GetString()!,
            Username = data.GetProperty("Username").GetString()!,
            Password = password,
            SyncIntervalSeconds = data.GetProperty("SyncIntervalSeconds").GetInt32(),
            AutoSync = data.GetProperty("AutoSync").GetBoolean(),
            EnableEncryption = data.GetProperty("EnableEncryption").GetBoolean()
        };
    }
}
```

---

## 错误处理和重试

### 重试策略

```csharp
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;

    public RetryPolicy(int maxRetries = 3, TimeSpan? initialDelay = null, 
        double backoffMultiplier = 2.0)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        _backoffMultiplier = backoffMultiplier;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (IsRetryable(ex))
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(
                        _initialDelay.TotalMilliseconds * 
                        Math.Pow(_backoffMultiplier, attempt));
                    await Task.Delay(delay);
                }
            }
        }
        
        throw lastException!;
    }
    
    private bool IsRetryable(HttpRequestException ex)
    {
        // 网络错误、超时、5xx 错误可重试
        return ex.Message.Contains("timeout") ||
               ex.Message.Contains("network") ||
               ex.InnerException is SocketException;
    }
}
```

### 离线队列

```csharp
public class OfflineQueue
{
    private readonly Queue<ChangeRecord> _pendingChanges = new();
    
    public void Enqueue(ChangeRecord change)
    {
        _pendingChanges.Enqueue(change);
    }
    
    public async Task ProcessQueueAsync(ISyncService syncService)
    {
        if (!IsNetworkAvailable())
            return;
        
        while (_pendingChanges.Count > 0)
        {
            var change = _pendingChanges.Dequeue();
            try
            {
                await syncService.SyncToRemoteAsync();
            }
            catch
            {
                // 同步失败，重新入队
                _pendingChanges.Enqueue(change);
                break;
            }
        }
    }
    
    private bool IsNetworkAvailable()
    {
        return NetworkInterface.GetIsNetworkAvailable();
    }
}
```

---

## 性能优化

### 1. 增量同步（基于序列号）

```csharp
// 只同步序列号大于本地最大序列号的变更
public async Task SyncFromRemoteAsync()
{
    // 读取 sync 文件夹中的所有同步 Item
    var allSyncItems = await ReadAllSyncItemsAsync();
    var localMaxSeq = await _changeLogService.GetMaxSyncedSequenceNumberAsync() ?? 0;
    
    // 筛选未同步的变更
    var unsyncedItems = allSyncItems
        .Where(x => x.SequenceNumber > localMaxSeq)
        .OrderBy(x => x.SequenceNumber)
        .ToList();
    
    await ApplySyncItemsAsync(unsyncedItems);
}
```

### 2. 压缩传输（可选）

```csharp
// 使用 GZip 压缩单个变更记录的 JSON 数据
private async Task UploadChangeFileAsync(string filePath, ChangeRecord change)
{
    var json = JsonSerializer.Serialize(change, new JsonSerializerOptions 
    { 
        WriteIndented = false  // 压缩格式
    });
    
    // 可选：压缩数据（如果变更记录较大）
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    // 如果启用压缩
    // var compressed = CompressGZip(json);
    // var content = new ByteArrayContent(compressed);
    // content.Headers.ContentEncoding.Add("gzip");
    
    var response = await _httpClient.PutAsync(filePath, content);
    response.EnsureSuccessStatusCode();
}

private byte[] CompressGZip(string data)
{
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
    {
        using var writer = new StreamWriter(gzip, Encoding.UTF8);
        writer.Write(data);
    }
    return output.ToArray();
}
```

### 3. 文件清理（可选）

由于每个同步 Item 是一个文件，文件数量会逐渐增加。可以定期清理旧的同步 Item：

```csharp
/// <summary>
/// 清理旧的同步 Item 文件（保留最近 N 天的记录）
/// </summary>
public async Task CleanupOldSyncItemsAsync(TimeSpan retentionPeriod)
{
    var syncFolderPath = $"{_webDavUrl}/sync/";
    var files = await ListFilesInFolderAsync(syncFolderPath);
    var cutoffDate = DateTime.UtcNow - retentionPeriod;
    
    foreach (var fileName in files)
    {
        if (!fileName.EndsWith(".json"))
            continue;
        
        try
        {
            var filePath = $"{syncFolderPath}{fileName}";
            var syncItem = await ReadSyncItemFileAsync(filePath);
            
            if (syncItem != null && syncItem.Timestamp < cutoffDate)
            {
                // 删除旧文件
                var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, filePath);
                var response = await _httpClient.SendAsync(deleteRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Deleted old sync item file {FileName}", fileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup file {FileName}", fileName);
        }
    }
}
```

### 4. 批量操作优化

```csharp
// 批量读取文件，减少网络请求次数（已在 ReadAllSyncItemsAsync 中实现）
// 使用 SemaphoreSlim 限制并发数，避免过多并发请求
```

---

## 部署建议

### WebDAV 服务器选择

1. **NAS 设备**（推荐）
   - Synology、QNAP 等 NAS 自带 WebDAV
   - 数据完全在本地，隐私性最好
   - 需要公网 IP 或内网穿透
   - **文件位置**：在 WebDAV 根目录创建 `sync_changes.json` 文件

2. **云存储服务**
   - Nextcloud（开源，可自建）
   - iCloud Drive（支持 WebDAV）
   - 其他支持 WebDAV 的云存储
   - **文件位置**：在 WebDAV 根目录创建 `sync_changes.json` 文件

3. **自建服务器**
   - Apache + mod_dav
   - Nginx + nginx-dav-ext-module
   - 使用 Docker 快速部署
   - **文件位置**：确保 WebDAV 服务器支持 PUT/GET 操作，文件路径为 `/sync_changes.json`

### 文件夹初始化

首次使用时，代码会自动创建 `sync/` 文件夹：

```
WebDAV 根目录/
  └─ sync/                  ← 自动创建（如果不存在）
      ├─ {syncItemId1}.json ← 自动创建
      ├─ {syncItemId2}.json
      ├─ {syncItemId3}.json
      └─ ...
```

**自动创建机制**：
- 代码会在首次同步时自动创建 `sync/` 文件夹（使用 MKCOL）
- 每个同步 Item 会自动创建对应的 JSON 文件
- 无需手动创建任何文件夹或文件

### 客户端配置步骤

1. **获取 WebDAV 服务器信息**
   - 服务器 URL（如：`https://webdav.example.com`）
   - 用户名和密码

2. **配置同步设置**
   - 在应用设置中输入 WebDAV 信息
   - 选择同步的表（剪贴板、收藏等）
   - 设置同步间隔

3. **测试连接**
   - 点击"测试连接"按钮
   - 验证是否能正常访问 WebDAV

4. **开始同步**
   - 首次同步会上传所有本地数据
   - 后续自动增量同步

---

## 监控和日志

### 同步状态

```csharp
public class SyncStatus
{
    public bool IsSyncing { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int PendingUploadCount { get; set; }
    public int PendingDownloadCount { get; set; }
    public string? LastError { get; set; }
    public SyncHealth Health { get; set; }
}

public enum SyncHealth
{
    Healthy,      // 正常
    Warning,      // 警告（如：同步延迟）
    Error         // 错误（如：连接失败）
}
```

### 日志记录

```csharp
// 记录同步操作
_logger.LogInformation("Sync started. Upload: {UploadCount}, Download: {DownloadCount}", 
    uploadCount, downloadCount);

_logger.LogWarning("Sync failed: {Error}", errorMessage);

_logger.LogError(ex, "Unexpected error during sync");
```

---

## 总结

WebDAV 同步方案特别适合需要**隐私保护**的数据同步场景：

- ✅ **用户完全控制**：数据存储在用户自己的服务器
- ✅ **标准协议**：WebDAV 广泛支持，兼容性好
- ✅ **增量同步**：只同步变更，效率高
- ✅ **离线支持**：变更本地缓存，网络恢复后自动同步
- ✅ **安全性**：支持 HTTPS 和认证，可选的端到端加密

**推荐使用场景**：
- 剪贴板历史同步
- 收藏夹/书签同步
- 个人笔记同步
- 其他隐私敏感数据

**不推荐使用场景**：
- 需要实时同步的应用（延迟较高）
- 大数据量频繁变更（WebDAV 性能限制）
- 需要复杂冲突解决策略（需要额外实现）
