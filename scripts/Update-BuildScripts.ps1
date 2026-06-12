#!/usr/bin/env pwsh
# Regenerate build.ps1 wrappers for remaining wpf-demos projects (co-detect only).

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$entries = @(
    @{ Dir = 'co-detect'; Name = 'CoDetectNet.Desktop'; Config = 'build-desktop.yaml'; Project = 'src\CoDetectNet.Desktop'; File = 'build-desktop.ps1' },
    @{ Dir = 'co-detect'; Name = 'CoDetectNet.Server'; Config = 'build-server.yaml'; Project = 'src\CoDetectNet.Server'; File = 'build-server.ps1' }
)

function New-BuildPs1Content {
    param([string]$Name, [string]$Config, [string]$Project)
    @"
#!/usr/bin/env pwsh
# Build $Name via qkbuild (see README.md)

param(
    [Alias('p')]
    [switch]`$Publish,
    [Alias('n')]
    [switch]`$NoVersion,
    [Alias('t')]
    [switch]`$Test,
    [Parameter(ValueFromRemainingArguments = `$true)]
    [object[]]`$QkbuildArgs
)

`$ErrorActionPreference = 'Stop'
`$invoke = Join-Path (Split-Path `$PSScriptRoot -Parent) 'scripts\Invoke-Qkbuild.ps1'
& `$invoke `
    -ProjectRoot `$PSScriptRoot `
    -ConfigFile '$Config' `
    -ProjectPath '$Project' `
    -Publish:`$Publish `
    -NoVersion:`$NoVersion `
    -Test:`$Test `
    -QkbuildArgs `$QkbuildArgs
exit `$LASTEXITCODE
"@
}

foreach ($e in $entries) {
    $file = if ($e.File) { $e.File } else { 'build.ps1' }
    $path = Join-Path (Join-Path $root $e.Dir) $file
    if (-not (Test-Path -LiteralPath (Split-Path $path -Parent)) {
        Write-Host "Skip (dir missing): $path" -ForegroundColor DarkGray
        continue
    }
    Set-Content -Path $path -Value (New-BuildPs1Content -Name $e.Name -Config $e.Config -Project $e.Project) -Encoding utf8
    Write-Host "Updated $path"
}
