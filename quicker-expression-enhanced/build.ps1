#!/usr/bin/env pwsh
# Build QuickerExpressionEnhanced using qkbuild

Write-Host "Building QuickerExpressionEnhanced..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\QuickerExpressionEnhanced" @args

