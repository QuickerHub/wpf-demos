#!/usr/bin/env pwsh
# Build WpfLottery using qkbuild

Write-Host "Building WpfLottery..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WpfLottery" @args

