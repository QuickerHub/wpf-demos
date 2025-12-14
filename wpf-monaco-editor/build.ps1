#!/usr/bin/env pwsh
# Build WpfMonacoEditor using qkbuild

Write-Host "Building WpfMonacoEditor..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WpfMonacoEditor" @args

