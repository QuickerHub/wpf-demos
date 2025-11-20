#!/usr/bin/env pwsh
# Build QuickerExpressionAgent.Quicker using qkbuild

Write-Host "Building QuickerExpressionAgent.Quicker..." -ForegroundColor Cyan
qkbuild build -c "build-quicker.yaml" --project-path "src\QuickerExpressionAgent.Quicker" @args

