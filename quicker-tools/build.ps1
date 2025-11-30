#!/usr/bin/env pwsh
# Build QuickerTools using qkbuild

Write-Host "Building QuickerTools..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerTools" @args

