#!/usr/bin/env pwsh
# Build VscodeTools using qkbuild

Write-Host "Building VscodeTools..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\VscodeTools" @args

