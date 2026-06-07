#!/usr/bin/env pwsh
# Build QuickerExpressionAgent (Quicker + Desktop). See also build-quicker.ps1 / build-desktop.ps1.

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
$common = @{}
if ($Publish) { $common.Publish = $true }
if ($NoVersion) { $common.NoVersion = $true }
if ($Test) { $common.Test = $true }
& "$PSScriptRoot\build-quicker.ps1" @common -QkbuildArgs $QkbuildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$PSScriptRoot\build-desktop.ps1" @common -QkbuildArgs $QkbuildArgs
exit $LASTEXITCODE
