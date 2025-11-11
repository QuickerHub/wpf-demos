#!/usr/bin/env pwsh
# Build QuickerCodeEditor using qkbuild

Write-Host "Building QuickerCodeEditor..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerCodeEditor" @args

