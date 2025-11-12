#!/usr/bin/env pwsh
# Build WpfGobang using qkbuild

Write-Host "Building WpfGobang..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WpfGobang" @args

