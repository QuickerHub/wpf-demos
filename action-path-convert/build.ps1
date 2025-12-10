#!/usr/bin/env pwsh
# Build ActionPathConvert using qkbuild

Write-Host "Building ActionPathConvert..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\ActionPathConvert" @args

