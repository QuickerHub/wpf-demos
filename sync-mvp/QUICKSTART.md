# 快速开始指南

## 项目概述

这是一个数据库同步的 MVP（最小可行产品）项目，用于验证多客户端之间的数据同步机制。

## 功能特性

- ✅ 列表编辑功能（添加、编辑、删除）
- ✅ 自动保存（编辑后1秒自动保存）
- ✅ 变更日志记录
- ✅ WebDAV 同步（简化版）
- ✅ 自动同步（每30秒）
- ✅ 手动同步按钮

## 快速开始

### 1. 构建项目

```bash
cd sync-mvp
dotnet build
```

### 2. 配置 WebDAV URL（可选）

默认使用 `http://localhost:8080/webdav`，可以通过环境变量修改：

```powershell
# Windows PowerShell
$env:WEBDAV_URL = "http://your-webdav-server.com/webdav"

# Linux/Mac
export WEBDAV_URL="http://your-webdav-server.com/webdav"
```

### 3. 运行应用

```bash
dotnet run --project SyncMvp.Wpf
```

## 使用说明

### 添加项

1. 在顶部输入框输入文本
2. 点击 "Add" 按钮或按 Enter 键

### 编辑项

1. 直接在 TextBox 中修改文本
2. 停止输入1秒后自动保存

### 删除项

1. 点击项右侧的红色 "Delete" 按钮

### 同步

- **自动同步**：每30秒自动同步一次
- **手动同步**：点击底部的 "Sync Now" 按钮

## 项目结构

```
sync-mvp/
├── SyncMvp.Core/              # 核心业务逻辑
│   ├── Models/                # 数据模型
│   │   └── CommonItem.cs      # 列表项模型
│   ├── Services/              # 业务服务
│   │   └── ItemService.cs     # 列表项服务
│   └── Sync/                  # 同步相关
│       ├── ChangeRecord.cs     # 变更记录
│       ├── ChangeLogService.cs # 变更日志服务
│       └── WebDavSyncService.cs # WebDAV 同步服务
├── SyncMvp.Wpf/               # WPF 客户端
│   ├── ViewModels/            # MVVM ViewModel
│   │   └── MainWindowViewModel.cs
│   ├── MainWindow.xaml        # 主窗口
│   └── App.xaml.cs           # 应用程序入口
└── README.md                  # 项目说明
```

## 数据存储

- **数据库文件**：`%LocalAppData%\SyncMvp\database.db`
- **数据库类型**：SQLite
- **ORM**：FreeSql

## 同步机制

### 变更记录流程

1. **本地操作**：添加/编辑/删除项时，记录到 `ChangeRecord` 表
2. **上传变更**：将未同步的变更上传到 WebDAV 服务器
3. **下载变更**：从 WebDAV 下载其他客户端的变更
4. **应用变更**：按时间戳和序列号顺序应用变更
5. **标记已同步**：将已同步的变更标记为 `IsSynced = true`

### 冲突解决

当前使用简单的版本号（Version）比较：
- 如果远程版本 > 本地版本，则应用远程变更
- 否则保留本地版本

## 当前限制

1. **WebDAV 实现简化**：当前使用简化的 WebDAV 客户端，生产环境需要完整的 PROPFIND 支持
2. **冲突解决简单**：仅使用版本号比较，可能需要更复杂的策略
3. **无离线队列**：网络断开时无法同步
4. **无同步状态UI**：缺少详细的同步状态显示

## 下一步改进

### 短期（MVP 验证）

- [ ] 完善 WebDAV PROPFIND 实现
- [ ] 添加同步状态指示器
- [ ] 添加错误处理和重试机制
- [ ] 添加同步日志查看功能

### 中期（功能完善）

- [ ] 实现离线队列
- [ ] 改进冲突解决策略（Last Write Wins / Manual Merge）
- [ ] 添加同步配置界面
- [ ] 支持多表同步

### 长期（生产就绪）

- [ ] 迁移到 Dotmim.Sync
- [ ] 添加单元测试和集成测试
- [ ] 性能优化（批量同步、压缩）
- [ ] 安全性增强（加密传输、认证）

## 测试建议

### 单客户端测试

1. 运行应用
2. 添加几个项
3. 编辑项
4. 删除项
5. 检查数据库文件是否正确保存

### 多客户端测试

1. 启动两个应用实例（不同目录或不同机器）
2. 在客户端A添加项
3. 等待自动同步或手动同步
4. 检查客户端B是否能看到新项
5. 在客户端B编辑项
6. 检查客户端A是否能看到更新

### WebDAV 服务器测试

可以使用以下 WebDAV 服务器进行测试：

1. **Nextcloud**：https://nextcloud.com/
2. **ownCloud**：https://owncloud.com/
3. **Apache HTTP Server**：配置 WebDAV 模块
4. **IIS**：启用 WebDAV 功能

## 故障排除

### 同步失败

1. 检查 WebDAV URL 是否正确
2. 检查网络连接
3. 查看控制台日志输出
4. 检查 WebDAV 服务器是否可访问

### 数据不同步

1. 检查变更日志表是否有未同步的记录
2. 手动触发同步
3. 检查版本号冲突
4. 查看同步日志

## 相关文档

- [README.md](README.md) - 项目说明
- [SYNC_SCHEMES.md](SYNC_SCHEMES.md) - 同步方案对比
