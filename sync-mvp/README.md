# WebDAV 数据同步项目

## 项目概述

基于 WebDAV 协议的 SQLite 数据库增量同步方案，特别适用于**剪贴板、收藏等隐私数据**的跨设备同步。

### 核心特性

- ✅ **隐私保护**：数据存储在用户自己的 WebDAV 服务器，完全由用户控制
- ✅ **无需第三方服务**：用户配置自己的 WebDAV 服务器（NAS、云存储等）
- ✅ **增量同步**：只同步变更记录，效率高
- ✅ **离线支持**：变更记录本地缓存，网络恢复后自动同步
- ✅ **多设备支持**：支持多客户端同时使用

## 技术方案

本项目采用 **变更日志 + WebDAV** 方案：

- **变更记录**：每次数据变更（增删改）都记录到本地 `ChangeRecord` 表
- **WebDAV 传输**：通过 WebDAV 协议上传/下载变更记录
- **增量同步**：只同步未同步的变更，减少网络传输
- **冲突解决**：使用版本号机制，Last Write Wins 策略

### 为什么选择 WebDAV？

1. **隐私保护**：数据存储在用户自己的服务器，不经过第三方
2. **用户控制**：用户完全控制数据存储位置和访问权限
3. **标准协议**：WebDAV 是标准 HTTP 扩展，兼容性好
4. **易于部署**：NAS、云存储都支持 WebDAV，无需额外服务器
5. **适合隐私数据**：剪贴板、收藏等敏感数据的最佳选择

## 详细方案文档

📖 **[WebDAV 数据同步方案文档](./WEBDAV_SYNC_SCHEME.md)** - 完整的技术方案、实现细节和最佳实践

## 项目结构

```
sync-mvp/
├── SyncMvp.Core/          # 核心业务逻辑
│   ├── Models/           # 数据模型
│   ├── Services/         # 业务服务
│   └── Sync/             # 同步相关
├── SyncMvp.Wpf/          # WPF 客户端
│   ├── ViewModels/       # MVVM ViewModel
│   ├── Views/            # XAML 视图
│   └── Services/         # UI 服务
├── SyncMvp.Server/       # WebDAV 服务器
│   └── Program.cs        # 服务器入口
└── README.md             # 项目说明
```

## 技术栈

- **.NET 8.0**：目标框架
- **WPF**：UI 框架
- **SQLite**：本地数据库（使用 FreeSql）
- **WebDAV**：同步传输协议
- **MVVM**：架构模式（CommunityToolkit.Mvvm）

## 快速开始

### 1. 构建项目

```bash
cd sync-mvp
dotnet build
```

### 2. 启动 WebDAV 服务器

在一个终端窗口中运行：

```powershell
# 使用脚本（推荐）
.\run-server.ps1

# 或直接使用 dotnet 命令
dotnet run --project SyncMvp.Server
```

服务器将在 `http://localhost:8080` 启动。

### 3. 运行客户端

在另一个终端窗口中运行：

```powershell
# 使用脚本（推荐）
.\run-client.ps1

# 或直接使用 dotnet 命令
dotnet run --project SyncMvp.Wpf
```

客户端默认连接到 `http://localhost:8080/webdav`。

### 4. 多客户端测试

可以运行多个客户端实例进行测试：
- 在不同目录运行，或
- 在不同机器上运行（需要修改 WebDAV URL）

```bash
# 修改 WebDAV URL（如果需要）
$env:WEBDAV_URL = "http://your-server-ip:8080/webdav"
dotnet run --project SyncMvp.Wpf
```

## 功能说明

1. **添加项**：在顶部输入框输入文本，点击 "Add" 或按 Enter
2. **编辑项**：直接在 TextBox 中修改文本，1秒后自动保存
3. **删除项**：点击项右侧的 "Delete" 按钮
4. **同步**：点击 "Sync Now" 手动同步，或等待自动同步（30秒间隔）

## 同步机制

1. **变更记录**：每次增删改操作都会记录到 `ChangeRecord` 表
2. **上传变更**：将未同步的变更上传到 WebDAV 服务器
3. **下载变更**：从 WebDAV 下载其他客户端的变更
4. **应用变更**：按时间戳和序列号顺序应用变更
5. **冲突解决**：使用版本号（Version）进行简单冲突检测

## 下一步改进

1. ✅ 实现基本的变更日志同步
2. ⏳ 完善 WebDAV PROPFIND 实现（当前为简化版本）
3. ⏳ 添加冲突解决策略配置
4. ⏳ 添加同步状态指示器
5. ⏳ 支持离线模式
6. ⏳ 添加单元测试

## 文档说明

- **[WEBDAV_SYNC_SCHEME.md](./WEBDAV_SYNC_SCHEME.md)** - 完整的 WebDAV 同步方案文档
  - 架构设计
  - 数据模型
  - 核心实现
  - 安全性考虑
  - 配置管理
  - 错误处理和重试
  - 性能优化
  - 部署建议

## 注意事项

- 当前实现是 MVP 版本，生产环境需要完善错误处理和安全性
- 数据库文件存储在 `%LocalAppData%\SyncMvp\database.db`
- WebDAV 服务器需要用户自行配置（NAS、Nextcloud 等）
