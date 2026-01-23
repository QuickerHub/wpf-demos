#!/usr/bin/env pwsh
# Build XmlExtractTool using qkbuild

Write-Host "Building XmlExtractTool..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\XmlExtractTool" @args
