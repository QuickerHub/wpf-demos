#!/usr/bin/env pwsh
# Build QuickerActionManage using qkbuild

Write-Host "Building QuickerActionManage..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerActionManage" @args

