#!/usr/bin/env pwsh
# Build WpfWebview using qkbuild

Write-Host "Building WpfWebview..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WpfWebview" @args

