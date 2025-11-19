# Demo script for QuickerExpressionAgent.Demo
# Run the demo console application

$ErrorActionPreference = "Stop"

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "src\QuickerExpressionAgent.Demo\QuickerExpressionAgent.Demo.csproj"

# Check if project exists
if (-not (Test-Path $projectPath)) {
    Write-Host "Error: Project not found at $projectPath" -ForegroundColor Red
    exit 1
}

# Run the project
Write-Host "Running QuickerExpressionAgent.Demo..." -ForegroundColor Green
Write-Host ""

dotnet run --project $projectPath

