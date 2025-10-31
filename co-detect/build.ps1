#!/usr/bin/env pwsh
# Build CoDetectNet.Server using qkbuild

Write-Host "Building CoDetectNet.Server..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\CoDetectNet.Server" @args

