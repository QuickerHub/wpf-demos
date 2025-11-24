#!/usr/bin/env pwsh
# Publish QuickerExpressionAgent.Desktop as self-contained single-file

param(
    [string]$OutputPath = "publish\QuickerExpressionAgent.Desktop",
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

# Get project path
$projectPath = "src\QuickerExpressionAgent.Desktop\QuickerExpressionAgent.Desktop.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "Error: Project file not found: $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing QuickerExpressionAgent.Desktop..." -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Runtime: $RuntimeIdentifier" -ForegroundColor Gray
Write-Host "  Output: $OutputPath" -ForegroundColor Gray
Write-Host "  Self-contained: true" -ForegroundColor Gray
Write-Host "  Single-file: true" -ForegroundColor Gray
Write-Host ""

# Clean previous publish output
if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous publish output..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Publish with self-contained and single-file
# Note: -c $Configuration ensures Release configuration is used
# Command-line parameters override .csproj settings
dotnet publish `
    $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Publish completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $OutputPath" -ForegroundColor Green

# Show output file info
$exePath = Join-Path $OutputPath "QuickerExpressionAgent.Desktop.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "Executable: $exePath" -ForegroundColor Green
    Write-Host "Size: $fileSizeMB MB" -ForegroundColor Green
}

