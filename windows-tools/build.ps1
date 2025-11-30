#!/usr/bin/env pwsh
# Build WindowsTools using qkbuild

Write-Host "Building WindowsTools..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WindowsTools" @args

