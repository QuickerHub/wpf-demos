#!/usr/bin/env pwsh
# Build CoDetectNet.Desktop using qkbuild

Write-Host "Building CoDetectNet.Desktop..." -ForegroundColor Cyan
qkbuild build -c "build-desktop.yaml" --project-path "src\CoDetectNet.Desktop" @args

