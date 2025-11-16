#!/usr/bin/env pwsh
# Build QuickerActionPanel using qkbuild

Write-Host "Building QuickerActionPanel..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerActionPanel" @args

