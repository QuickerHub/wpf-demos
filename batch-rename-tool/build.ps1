#!/usr/bin/env pwsh
# Build BatchRenameTool using qkbuild

Write-Host "Building BatchRenameTool..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\BatchRenameTool" @args
