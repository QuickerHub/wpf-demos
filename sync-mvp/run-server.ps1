# WebDAV Server Startup Script
# 启动 WebDAV 服务器

$ErrorActionPreference = "Stop"

Write-Host "Starting WebDAV Server..." -ForegroundColor Green
Write-Host "Server will run on http://localhost:8080" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host ""

try {
    dotnet run --project SyncMvp.Server
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
