#!/usr/bin/env pwsh
# Run StepsChanger application

param(
    [string]$Configuration = "Debug",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "src\StepsChanger"
$ProjectFile = Join-Path $ProjectDir "StepsChanger.csproj"

# Output directory
$OutputDir = Join-Path $ProjectDir "bin\$Configuration\net472"
$ExePath = Join-Path $OutputDir "StepsChanger.exe"

Write-Host "StepsChanger Runner" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""

# Check if project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Host "Error: Project file not found: $ProjectFile" -ForegroundColor Red
    exit 1
}

# Build if needed
if (-not $NoBuild) {
    Write-Host "Building project..." -ForegroundColor Yellow
    
    $buildArgs = @(
        "build",
        $ProjectFile,
        "--configuration", $Configuration,
        "--verbosity", "minimal"
    )
    
    dotnet @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Build completed successfully." -ForegroundColor Green
    Write-Host ""
}

# Check if executable exists
if (-not (Test-Path $ExePath)) {
    Write-Host "Error: Executable not found: $ExePath" -ForegroundColor Red
    Write-Host "Please build the project first or check the configuration." -ForegroundColor Yellow
    exit 1
}

# Run the application
Write-Host "Starting StepsChanger..." -ForegroundColor Green
Write-Host "Executable: $ExePath" -ForegroundColor Gray
Write-Host ""

& $ExePath

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Application exited with code: $LASTEXITCODE" -ForegroundColor Yellow
}

