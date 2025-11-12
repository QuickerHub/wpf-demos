#!/usr/bin/env pwsh
# Build WindowAttach using qkbuild

Write-Host "Building WindowAttach..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WindowAttach" @args

