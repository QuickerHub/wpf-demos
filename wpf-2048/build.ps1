#!/usr/bin/env pwsh
# Build Wpf2048 using qkbuild

Write-Host "Building Wpf2048..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\Wpf2048" @args

