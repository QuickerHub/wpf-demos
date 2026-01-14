#!/usr/bin/env pwsh
# Build QuickerScreenshot using qkbuild

Write-Host "Building QuickerScreenshot..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerScreenshot" @args
