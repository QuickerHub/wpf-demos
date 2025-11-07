#!/usr/bin/env pwsh
# Build StepsChanger using qkbuild

Write-Host "Building StepsChanger..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\StepsChanger" @args

