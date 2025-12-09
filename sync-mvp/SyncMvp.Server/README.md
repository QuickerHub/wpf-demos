# WebDAV Server

简单的 WebDAV 服务器，用于同步 MVP 项目的测试。

## 运行

```bash
cd SyncMvp.Server
dotnet run
```

服务器将在 `http://localhost:8080` 启动。

## 存储位置

文件存储在 `webdav-storage` 目录下，结构如下：

```
webdav-storage/
└── changes/
    ├── {clientId1}/
    │   ├── 20240101120000_changes.json
    │   └── 20240101120100_changes.json
    └── {clientId2}/
        └── 20240101120030_changes.json
```

## API

- `GET /webdav/{path}` - 下载文件
- `PUT /webdav/{path}` - 上传文件
- `DELETE /webdav/{path}` - 删除文件
- `PROPFIND /webdav/{path}` - 列出目录内容（返回 JSON）
- `MKCOL /webdav/{path}` - 创建目录

## 客户端配置

客户端默认连接到 `http://localhost:8080/webdav`，可以通过环境变量修改：

```bash
$env:WEBDAV_URL = "http://localhost:8080/webdav"
```
