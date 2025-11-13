#!/usr/bin/env pwsh
# Build WindowEdgeHide using qkbuild

Write-Host "Building WindowEdgeHide..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WindowEdgeHide" @args

