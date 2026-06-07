#!/usr/bin/env pwsh
# Build CoDetectNet.Desktop via qkbuild (see wpf-demos/README.md)

param(
    [Alias('p')]
    [switch]$Publish,
    [Alias('n')]
    [switch]$NoVersion,
    [Alias('t')]
    [switch]$Test,
    [Parameter(ValueFromRemainingArguments = $true)]
    [object[]]$QkbuildArgs
)

$ErrorActionPreference = 'Stop'
$invoke = Join-Path (Split-Path $PSScriptRoot -Parent) 'scripts\Invoke-Qkbuild.ps1'
& $invoke 
    -ProjectRoot $PSScriptRoot 
    -ConfigFile 'build-desktop.yaml' 
    -ProjectPath 'src\CoDetectNet.Desktop' 
    -Publish:$Publish 
    -NoVersion:$NoVersion 
    -Test:$Test 
    -QkbuildArgs $QkbuildArgs
exit $LASTEXITCODE
