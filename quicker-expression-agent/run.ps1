# Run Quicker Expression Agent Server
# This script builds and runs the Server project

$ErrorActionPreference = "Stop"

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverProject = Join-Path $scriptDir "src\QuickerExpressionAgent.Server\QuickerExpressionAgent.Server.csproj"

Write-Host "Building Server project..." -ForegroundColor Cyan
dotnet build $serverProject --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Running Server..." -ForegroundColor Green
Write-Host ""

# Run the server
dotnet run --project $serverProject

