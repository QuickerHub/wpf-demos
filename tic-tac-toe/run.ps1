# TicTacToe 游戏启动脚本
# PowerShell script to run the TicTacToe game

Write-Host "🎮 启动井字棋游戏..." -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

# 检查 .NET 是否已安装
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command not found"
    }
    Write-Host "✅ .NET 版本: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ 错误: 未找到 .NET SDK" -ForegroundColor Red
    Write-Host "请先安装 .NET SDK: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Read-Host "按任意键退出"
    exit 1
}

# 检查项目文件是否存在
$projectFile = "src\TicTacToe\TicTacToe.csproj"
if (-not (Test-Path $projectFile)) {
    Write-Host "❌ 错误: 未找到项目文件 $projectFile" -ForegroundColor Red
    Read-Host "按任意键退出"
    exit 1
}

Write-Host "📁 项目文件: $projectFile" -ForegroundColor Green

# 构建项目
Write-Host "🔨 正在构建项目..." -ForegroundColor Yellow
dotnet build $projectFile --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 构建失败！" -ForegroundColor Red
    Write-Host "正在尝试 Debug 模式构建..." -ForegroundColor Yellow
    dotnet build $projectFile --configuration Debug --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 构建失败！请检查代码错误。" -ForegroundColor Red
        Read-Host "按任意键退出"
        exit 1
    }
}

Write-Host "✅ 构建成功！" -ForegroundColor Green

# 运行游戏
Write-Host "🚀 启动游戏..." -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host "游戏说明:" -ForegroundColor Yellow
Write-Host "- 你是 X，AI 是 O" -ForegroundColor White
Write-Host "- 选择难度后开始游戏" -ForegroundColor White
Write-Host "- 点击空位下棋" -ForegroundColor White
Write-Host "- 点击'重置游戏'开始新一局" -ForegroundColor White
Write-Host "================================" -ForegroundColor Cyan

# 运行游戏
dotnet run --project $projectFile --configuration Release

# 如果 Release 模式失败，尝试 Debug 模式
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Release 模式运行失败，尝试 Debug 模式..." -ForegroundColor Yellow
    dotnet run --project $projectFile --configuration Debug
}

Write-Host "`n🎮 游戏已结束，感谢游玩！" -ForegroundColor Green

