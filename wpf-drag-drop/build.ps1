#!/usr/bin/env pwsh
# Build WpfDragDrop using qkbuild

Write-Host "Building WpfDragDrop..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WpfDragDrop" @args

