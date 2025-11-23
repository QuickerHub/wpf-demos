#!/usr/bin/env pwsh
# Build QuickerExpressionAgent.Desktop using qkbuild

Write-Host "Building QuickerExpressionAgent.Desktop..." -ForegroundColor Cyan
qkbuild build -c "build-desktop.yaml" --project-path "src\QuickerExpressionAgent.Desktop" @args

