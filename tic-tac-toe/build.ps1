#!/usr/bin/env pwsh
# Build TicTacToe using qkbuild

Write-Host "Building TicTacToe..." -ForegroundColor Cyan
qkbuild build -c "build.yaml" --project-path "src\TicTacToe" @args
