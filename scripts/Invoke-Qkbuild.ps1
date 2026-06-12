#!/usr/bin/env pwsh
# Shared qkbuild wrapper for wpf-demos subprojects.

param(
    [Parameter(Mandatory)]
    [string]$ProjectRoot,

    [Parameter(Mandatory)]
    [string]$ConfigFile,

    [Parameter(Mandatory)]
    [string]$ProjectPath,

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

function Expand-QkbuildArgTokens {
    param([object[]]$Raw)
    $out = [System.Collections.Generic.List[string]]::new()
    foreach ($item in @($Raw)) {
        if ($null -eq $item) { continue }
        if ($item -is [System.Array]) {
            foreach ($sub in $item) {
                if (-not [string]::IsNullOrWhiteSpace([string]$sub)) {
                    $out.Add([string]$sub.Trim())
                }
            }
            continue
        }
        $text = [string]$item
        if ([string]::IsNullOrWhiteSpace($text)) { continue }
        foreach ($part in ($text -split '[\s,]+')) {
            if (-not [string]::IsNullOrWhiteSpace($part)) {
                $out.Add($part.Trim())
            }
        }
    }
    return $out
}

$extra = Expand-QkbuildArgTokens -Raw $QkbuildArgs
if ($Publish) { $extra = @('--publish', '-y') + $extra }
if ($NoVersion) { $extra = @('--no-version') + $extra }
if ($Test) { $extra = @('--test') + $extra }

$configPath = Join-Path $ProjectRoot $ConfigFile
$resolvedProject = Join-Path $ProjectRoot $ProjectPath

if (-not (Test-Path -LiteralPath $configPath)) {
    throw "build config not found: $configPath"
}

Write-Host "qkbuild: $ConfigFile -> $ProjectPath" -ForegroundColor Cyan
[Console]::Out.Flush()
if ($extra.Count -gt 0) {
    Write-Host "args: $($extra -join ' ')" -ForegroundColor DarkGray
    [Console]::Out.Flush()
}

Push-Location $ProjectRoot
try {
    $cmd = @(
        'build',
        '-c', $ConfigFile,
        '--project-path', $resolvedProject
    ) + $extra
    & qkbuild @cmd
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
