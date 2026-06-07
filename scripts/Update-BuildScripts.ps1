#!/usr/bin/env pwsh
# Regenerate per-project build.ps1 wrappers (run from wpf-demos root).

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$entries = @(
    @{ Dir = "action-path-convert"; Name = "ActionPathConvert"; Config = "build.yaml"; Project = "src\ActionPathConvert" },
    @{ Dir = "batch-rename-tool"; Name = "BatchRenameTool"; Config = "build.yaml"; Project = "src\BatchRenameTool" },
    @{ Dir = "cea-view-runner"; Name = "CeaViewRunner"; Config = "build.yaml"; Project = "src\CeaViewRunner" },
    @{ Dir = "co-detect"; Name = "CoDetectNet.Desktop"; Config = "build-desktop.yaml"; Project = "src\CoDetectNet.Desktop"; File = "build-desktop.ps1" },
    @{ Dir = "co-detect"; Name = "CoDetectNet.Server"; Config = "build-server.yaml"; Project = "src\CoDetectNet.Server"; File = "build-server.ps1" },
    @{ Dir = "quicker-action-manage"; Name = "QuickerActionManage"; Config = "build.yaml"; Project = "src\QuickerActionManage" },
    @{ Dir = "quicker-action-panel"; Name = "QuickerActionPanel"; Config = "build.yaml"; Project = "src\QuickerActionPanel" },
    @{ Dir = "quicker-code-editor"; Name = "QuickerCodeEditor"; Config = "build.yaml"; Project = "src\QuickerCodeEditor" },
    @{ Dir = "quicker-dev-tools"; Name = "quicker-dev-tools"; Config = "build.yaml"; Project = "src\quicker-dev-tools" },
    @{ Dir = "quicker-expression-agent"; Name = "QuickerExpressionAgent.Desktop"; Config = "build-desktop.yaml"; Project = "src\QuickerExpressionAgent.Desktop"; File = "build-desktop.ps1" },
    @{ Dir = "quicker-expression-agent"; Name = "QuickerExpressionAgent.Quicker"; Config = "build-quicker.yaml"; Project = "src\QuickerExpressionAgent.Quicker"; File = "build-quicker.ps1" },
    @{ Dir = "quicker-expression-enhanced"; Name = "QuickerExpressionEnhanced"; Config = "build.yaml"; Project = "src\QuickerExpressionEnhanced" },
    @{ Dir = "quicker-screenshot"; Name = "QuickerScreenshot"; Config = "build.yaml"; Project = "src\QuickerScreenshot" },
    @{ Dir = "quicker-statistics-info"; Name = "QuickerStatisticsInfo"; Config = "build.yaml"; Project = "src\QuickerStatisticsInfo" },
    @{ Dir = "quicker-tools"; Name = "QuickerTools"; Config = "build.yaml"; Project = "src\QuickerTools" },
    @{ Dir = "steps-changer"; Name = "StepsChanger"; Config = "build.yaml"; Project = "src\StepsChanger" },
    @{ Dir = "tic-tac-toe"; Name = "TicTacToe"; Config = "build.yaml"; Project = "src\TicTacToe" },
    @{ Dir = "vscode-tools"; Name = "VscodeTools"; Config = "build.yaml"; Project = "src\VscodeTools" },
    @{ Dir = "webview-markdown-tip"; Name = "WebViewMarkdownTip"; Config = "build.yaml"; Project = "src\WebViewMarkdownTip" },
    @{ Dir = "win-clip-tools"; Name = "WClipTools"; Config = "build.yaml"; Project = "src\WClipTools" },
    @{ Dir = "window-attach"; Name = "WindowAttach"; Config = "build.yaml"; Project = "src\WindowAttach" },
    @{ Dir = "window-edge-hide"; Name = "WindowEdgeHide"; Config = "build.yaml"; Project = "src\WindowEdgeHide" },
    @{ Dir = "windows-tools"; Name = "WindowsTools"; Config = "build.yaml"; Project = "src\WindowsTools" },
    @{ Dir = "wpf-2048"; Name = "Wpf2048"; Config = "build.yaml"; Project = "src\Wpf2048" },
    @{ Dir = "wpf-drag-drop"; Name = "WpfDragDrop"; Config = "build.yaml"; Project = "src\WpfDragDrop" },
    @{ Dir = "wpf-gobang"; Name = "WpfGobang"; Config = "build.yaml"; Project = "src\WpfGobang" },
    @{ Dir = "wpf-greedysnake"; Name = "WPFGreedySnake"; Config = "build.yaml"; Project = "src\WPFGreedySnake" },
    @{ Dir = "wpf-lottery"; Name = "WpfLottery"; Config = "build.yaml"; Project = "src\WpfLottery" },
    @{ Dir = "wpf-monaco-editor"; Name = "WpfMonacoEditor"; Config = "build.yaml"; Project = "src\WpfMonacoEditor" },
    @{ Dir = "wpf-webview"; Name = "WpfWebview"; Config = "build.yaml"; Project = "src\WpfWebview" },
    @{ Dir = "xml-extract-tool"; Name = "XmlExtractTool"; Config = "build.yaml"; Project = "src\XmlExtractTool" }
)

function New-BuildPs1Content {
    param([string]$Name, [string]$Config, [string]$Project)
    @"
#!/usr/bin/env pwsh
# Build $Name via qkbuild (see wpf-demos/README.md)

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
    Set-Content -Path $path -Value (New-BuildPs1Content -Name $e.Name -Config $e.Config -Project $e.Project) -Encoding utf8NoBOM
    Write-Host "Updated $path"
}

$both = @"
#!/usr/bin/env pwsh
# Build QuickerExpressionAgent (Quicker + Desktop). See also build-quicker.ps1 / build-desktop.ps1.

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
`$common = @{}
if (`$Publish) { `$common.Publish = `$true }
if (`$NoVersion) { `$common.NoVersion = `$true }
if (`$Test) { `$common.Test = `$true }
& "`$PSScriptRoot\build-quicker.ps1" @common -QkbuildArgs `$QkbuildArgs
if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE }
& "`$PSScriptRoot\build-desktop.ps1" @common -QkbuildArgs `$QkbuildArgs
exit `$LASTEXITCODE
"@
Set-Content -Path (Join-Path $root 'quicker-expression-agent\build.ps1') -Value $both -Encoding utf8NoBOM
Write-Host "Updated quicker-expression-agent\build.ps1"
