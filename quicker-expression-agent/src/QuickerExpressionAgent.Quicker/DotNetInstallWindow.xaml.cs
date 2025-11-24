using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualStudio.Threading;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Window for prompting user to install .NET 8.0 runtime
/// </summary>
public partial class DotNetInstallWindow : Window
{
    public DotNetInstallViewModel ViewModel { get; }

    public DotNetInstallWindow(string downloadUrl)
    {
        InitializeComponent();
        ViewModel = new DotNetInstallViewModel(this, downloadUrl);
        DataContext = ViewModel;
    }
}

public partial class DotNetInstallViewModel : ObservableObject
{
    private readonly string _downloadUrl;
    private readonly DotNetInstallWindow _window;
    private readonly Services.DotNetVersionChecker _dotNetVersionChecker;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage = false;

    public DotNetInstallViewModel(DotNetInstallWindow window, string downloadUrl)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _downloadUrl = downloadUrl ?? throw new ArgumentNullException(nameof(downloadUrl));
        _dotNetVersionChecker = Launcher.GetService<Services.DotNetVersionChecker>();
    }

    [RelayCommand]
    private async Task AutoInstall()
    {
        try
        {
            HasStatusMessage = true;
            StatusMessage = "正在尝试自动安装 .NET 8.0 Windows Desktop 运行时...";

            // Detect system architecture
            var architecture = GetSystemArchitecture();
            StatusMessage = $"检测到系统架构: {architecture}，正在安装 .NET 8.0.22 Windows Desktop 运行时...";

            // Method 1: Try using PowerShell to download and install (most reliable)
            if (await TryInstallWithPowerShellAsync(architecture))
            {
                StatusMessage = "正在使用 PowerShell 下载并安装 .NET 8.0 Windows Desktop 运行时...";
                return;
            }

            // Method 2: Try using winget (Windows Package Manager) as fallback
            if (await TryInstallWithWingetAsync(architecture))
            {
                StatusMessage = "正在使用 Windows Package Manager (winget) 安装 .NET 8.0 Windows Desktop 运行时...";
                return;
            }

            // Method 3: Fallback to manual download
            StatusMessage = "自动安装失败，请使用手动下载方式";
        }
        catch (Exception ex)
        {
            StatusMessage = $"自动安装失败: {ex.Message}。请使用手动下载方式。";
        }
    }

    private string GetSystemArchitecture()
    {
        // Check if 64-bit operating system
        if (Environment.Is64BitOperatingSystem)
        {
            // Further check if it's ARM64 or x64
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                return "arm64";
            }
            return "x64";
        }
        return "x86";
    }

    private async Task<bool> TryInstallWithWingetAsync(string architecture)
    {
        try
        {
            // Check if winget is available
            var checkProcess = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(checkProcess);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                return false;
            }

            // Create a temporary batch file to run winget install
            // This avoids issues with quotes and special characters in command line
            var tempDir = Path.Combine(Path.GetTempPath(), "QuickerExpressionAgent");
            Directory.CreateDirectory(tempDir);
            var batchFilePath = Path.Combine(tempDir, "install-dotnet8-desktop-runtime.bat");
            
            var batchScript = @"@echo off
echo 正在使用 winget 安装 .NET 8.0 Desktop Runtime...
echo.
winget install Microsoft.DotNet.DesktopRuntime.8 --accept-package-agreements --accept-source-agreements
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo 安装成功！
    echo ========================================
) else (
    echo.
    echo ========================================
    echo 安装失败，错误代码: %ERRORLEVEL%
    echo ========================================
)
echo.
echo 按任意键关闭此窗口...
pause >nul
";
            
            File.WriteAllText(batchFilePath, batchScript, System.Text.Encoding.Default);

            // Run the batch file in elevated mode
            var processStartInfo = new ProcessStartInfo
            {
                FileName = batchFilePath,
                UseShellExecute = true,
                Verb = "runas", // Run as administrator
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(processStartInfo);
            return true;
        }
        catch
        {
            // winget not available or failed
            return false;
        }
    }

    private async Task<bool> TryInstallWithPowerShellAsync(string architecture)
    {
        try
        {
            // .NET 8.0.22 Windows Desktop Runtime download URLs
            // These are the official download URLs for Windows Desktop Runtime 8.0.22
            string installerUrl;
            string installerFileName;
            
            if (architecture == "x64")
            {
                // Windows Desktop Runtime 8.0.22 x64 - Official download URL
                installerUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.22/windowsdesktop-runtime-8.0.22-win-x64.exe";
                installerFileName = "windowsdesktop-runtime-8.0.22-win-x64.exe";
            }
            else if (architecture == "x86")
            {
                // Windows Desktop Runtime 8.0.22 x86 - Official download URL
                installerUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.22/windowsdesktop-runtime-8.0.22-win-x86.exe";
                installerFileName = "windowsdesktop-runtime-8.0.22-win-x86.exe";
            }
            else
            {
                // ARM64 not commonly supported for Desktop Runtime, fallback to x64
                installerUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.22/windowsdesktop-runtime-8.0.22-win-x64.exe";
                installerFileName = "windowsdesktop-runtime-8.0.22-win-x64.exe";
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "QuickerExpressionAgent");
            Directory.CreateDirectory(tempDir);
            var installerPath = Path.Combine(tempDir, installerFileName);

            // PowerShell script to download and install with progress bar showing MB
            var psScript = $@"
$ErrorActionPreference = 'Stop'
Write-Host '正在下载 .NET 8.0.22 Windows Desktop 运行时 ({architecture})...' -ForegroundColor Yellow
Write-Host ''
try {{
    # Get total file size first
    $request = [System.Net.WebRequest]::Create('{installerUrl}')
    $request.Method = 'HEAD'
    $response = $request.GetResponse()
    $totalBytes = $response.ContentLength
    $response.Close()
    $totalMB = [math]::Round($totalBytes / 1MB, 2)
    
    Write-Host '文件大小: ' $totalMB ' MB' -ForegroundColor Cyan
    Write-Host '开始下载，请稍候...' -ForegroundColor Cyan
    Write-Host ''
    
    # Download with progress monitoring
    $ProgressPreference = 'SilentlyContinue'
    $uri = New-Object System.Uri('{installerUrl}')
    $webClient = New-Object System.Net.WebClient
    
    # Download in background job to monitor progress
    $job = Start-Job -ScriptBlock {{
        param($url, $outputPath)
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $outputPath)
        $wc.Dispose()
    }} -ArgumentList '{installerUrl}', '{installerPath}'
    
    # Monitor download progress
    $lastSize = 0
    while ($job.State -eq 'Running') {{
        Start-Sleep -Milliseconds 500
        if (Test-Path '{installerPath}') {{
            $currentSize = (Get-Item '{installerPath}').Length
            $downloadedMB = [math]::Round($currentSize / 1MB, 2)
            $percent = if ($totalBytes -gt 0) {{ [math]::Round(($currentSize / $totalBytes) * 100, 1) }} else {{ 0 }}
            Write-Progress -Activity '正在下载 .NET 8.0.22 Windows Desktop 运行时' -Status ('已下载: {{0}} MB / {{1}} MB ({{2}}%)' -f $downloadedMB, $totalMB, $percent) -PercentComplete $percent
            $lastSize = $currentSize
        }}
    }}
    
    # Wait for job to complete
    $job | Wait-Job | Out-Null
    $job | Remove-Job
    
    Write-Progress -Activity '正在下载 .NET 8.0.22 Windows Desktop 运行时' -Completed
    $webClient.Dispose()
    
    Write-Host ''
    Write-Host '下载完成，正在启动安装程序...' -ForegroundColor Green
    Write-Host '请按照安装程序的提示完成安装。' -ForegroundColor Yellow
    Write-Host ''
    
    # Start installer and wait for it to complete
    $installProcess = Start-Process -FilePath '{installerPath}' -Wait -Verb RunAs -PassThru
    
    if ($installProcess.ExitCode -eq 0 -or $installProcess.ExitCode -eq 3010) {{
        Write-Host '安装完成！' -ForegroundColor Green
    }} else {{
        Write-Host '安装完成（退出代码: ' $installProcess.ExitCode '）' -ForegroundColor Yellow
    }}
}} catch {{
    Write-Host ''
    Write-Host '安装失败: ' $_.Exception.Message -ForegroundColor Red
    Write-Host '请手动下载并安装 .NET 8.0.22 Windows Desktop 运行时' -ForegroundColor Yellow
}}
Write-Host ''
Write-Host '按任意键关闭此窗口...' -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
";

            // Save script to temp file
            var scriptPath = Path.Combine(tempDir, "install-dotnet8.ps1");
            File.WriteAllText(scriptPath, psScript, System.Text.Encoding.UTF8);

            // Run PowerShell script in elevated mode (without -NoExit, window will close after script completes)
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = true,
                Verb = "runas", // Run as administrator
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(processStartInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private void OpenDownloadPage()
    {
        try
        {
            // Get system architecture and build the download page URL
            var architecture = GetSystemArchitecture();
            string downloadPageUrl;
            
            if (architecture == "x64")
            {
                // .NET 8.0.22 Windows Desktop Runtime x64 download page
                downloadPageUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.22-windows-x64-installer";
            }
            else if (architecture == "x86")
            {
                // .NET 8.0.22 Windows Desktop Runtime x86 download page
                downloadPageUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.22-windows-x86-installer";
            }
            else
            {
                // ARM64 fallback to x64
                downloadPageUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.22-windows-x64-installer";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = downloadPageUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开下载页面: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CheckAndClose()
    {
        // Check if .NET 8.0+ is installed
        if (_dotNetVersionChecker.IsDotNet80Installed())
        {
            _window.DialogResult = true;
            _window.Close();
        }
        else
        {
            HasStatusMessage = true;
            StatusMessage = "未检测到 .NET 8.0+ 运行时，请先完成安装。";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.DialogResult = false;
        _window.Close();
    }

}

