# Run XmlExtractTool.Console. Optional: pass data folder path as first argument.
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$dataDir = Join-Path $ScriptDir "data"
if ($args.Count -gt 0 -and (Test-Path -LiteralPath $args[0] -PathType Container)) {
    $dataDir = (Resolve-Path -LiteralPath $args[0]).Path
}

& dotnet run --project src/XmlExtractTool.Console/XmlExtractTool.Console.csproj -- $dataDir
