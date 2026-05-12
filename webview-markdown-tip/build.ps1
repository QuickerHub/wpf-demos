#!/usr/bin/env pwsh
Write-Host "Building WebViewMarkdownTip..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WebViewMarkdownTip" @args
