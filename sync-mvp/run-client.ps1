# WPF Client Startup Script
# 启动 WPF 客户端

Write-Host "Starting WPF Client..." -ForegroundColor Green
Write-Host "Connecting to WebDAV server at http://localhost:8080/webdav" -ForegroundColor Yellow
Write-Host ""

# 可以设置自定义 WebDAV URL
if ($env:WEBDAV_URL) {
    Write-Host "Using custom WebDAV URL: $env:WEBDAV_URL" -ForegroundColor Cyan
}

dotnet run --project SyncMvp.Wpf
