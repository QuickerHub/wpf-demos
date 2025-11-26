#!/usr/bin/env pwsh
# Build quicker-dev-tools using qkbuild

Write-Host "Building quicker-dev-tools..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\quicker-dev-tools" @args

