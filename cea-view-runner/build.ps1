#!/usr/bin/env pwsh
Write-Host "Building CeaViewRunner..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\CeaViewRunner" @args
