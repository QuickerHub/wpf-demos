#!/usr/bin/env pwsh
# Build QuickerStatisticsInfo using qkbuild

Write-Host "Building QuickerStatisticsInfo..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerStatisticsInfo" @args

