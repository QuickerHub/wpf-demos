# Run script for CoDetectNet

$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = $scriptDir

# Build configuration
$config = "Debug"
if ($args.Count -gt 0 -and $args[0] -eq "Release") {
    $config = "Release"
}

Write-Host "Building CoDetectNet project..." -ForegroundColor Cyan
dotnet build "$projectDir/CoDetectNet.csproj" -c $config

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build successful!" -ForegroundColor Green

# Determine output directory
$outputDir = "$projectDir/bin/$config/net472"

# Verify required files exist in output directory
# Note: Files are automatically copied during build via .csproj Content items from resources directory
Write-Host "Verifying required files in output directory..." -ForegroundColor Cyan

$modelFile = "$outputDir/codetect.onnx"
$languagesFile = "$outputDir/languages.json"

if (Test-Path $modelFile) {
    Write-Host "  ✓ codetect.onnx found" -ForegroundColor Green
} else {
    Write-Host "  ✗ codetect.onnx not found at $modelFile" -ForegroundColor Red
    Write-Host "    Please ensure resources\codetect.onnx exists and rebuild the project" -ForegroundColor Yellow
}

if (Test-Path $languagesFile) {
    Write-Host "  ✓ languages.json found" -ForegroundColor Green
} else {
    Write-Host "  ✗ languages.json not found at $languagesFile" -ForegroundColor Red
    Write-Host "    Please ensure resources\languages.json exists and rebuild the project" -ForegroundColor Yellow
}

# Run the executable
Write-Host "`nRunning CoDetectNet..." -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Gray

Push-Location $outputDir
try {
    $exePath = "$outputDir/CoDetectNet.exe"
    if (Test-Path $exePath) {
        & $exePath
    } else {
        Write-Host "Executable not found: $exePath" -ForegroundColor Red
        Write-Host "Trying dotnet run instead..." -ForegroundColor Yellow
        Push-Location $projectDir
        dotnet run -c $config
        Pop-Location
    }
} finally {
    Pop-Location
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nExecution failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}

