#!/usr/bin/env pwsh
# Build WPFGreedySnake using qkbuild

Write-Host "Building WPFGreedySnake..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\WPFGreedySnake" @args
