# Script parameters
param(
    [Parameter(Position=0)]
    [string]$VMName,
    
    [Parameter(Position=1)]
    [string]$ShortcutName = $null,
    
    [switch]$ConnectOnly,
    [switch]$ListVMs,
    [switch]$CreateManager
)

# Initialize
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")

# Create Hyper-V Manager shortcut if requested
if ($CreateManager) {
    $ShortcutPath = Join-Path $DesktopPath "Hyper-V Manager.lnk"
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = "$env:WINDIR\System32\mmc.exe"
    $Shortcut.Arguments = "$env:WINDIR\System32\virtmgmt.msc"
    $Shortcut.Description = "Hyper-V Manager"
    $Shortcut.IconLocation = "$env:WINDIR\System32\virtmgmt.msc,0"
    $Shortcut.Save()
    Write-Host "Hyper-V Manager shortcut created at: $ShortcutPath" -ForegroundColor Green
    exit 0
}

# Example: Create shortcut for a specific VM
# Replace "YourVMName" with your actual VM name
function Create-VMShortcut {
    param(
        [string]$VMName,
        [string]$ShortcutName = $VMName
    )
    
    $DesktopPath = [Environment]::GetFolderPath("Desktop")
    $ShortcutPath = Join-Path $DesktopPath "$ShortcutName.lnk"
    
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = "$env:WINDIR\System32\vmconnect.exe"
    $Shortcut.Arguments = "localhost `"$VMName`""
    $Shortcut.Description = "Connect to $VMName"
    $Shortcut.IconLocation = "$env:WINDIR\System32\vmconnect.exe,0"
    $Shortcut.Save()
    
    Write-Host "VM shortcut created at: $ShortcutPath" -ForegroundColor Green
}

# List all available VMs
function Get-VMList {
    Write-Host "`nAvailable Virtual Machines:" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    try {
        $vms = Get-VM -ErrorAction SilentlyContinue
        if ($vms) {
            $vms | ForEach-Object {
                Write-Host "  - $($_.Name) (State: $($_.State))" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  No VMs found or Hyper-V module not available." -ForegroundColor Red
            Write-Host "  Try running: Get-VM" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  Error: $_" -ForegroundColor Red
        Write-Host "  Make sure Hyper-V PowerShell module is installed." -ForegroundColor Gray
    }
    Write-Host ""
}

# Create shortcut that connects to a VM (opens VMConnect)
function Create-VMConnectShortcut {
    param(
        [Parameter(Mandatory=$true)]
        [string]$VMName,
        [string]$ShortcutName = $VMName,
        [string]$OutputPath = $null
    )
    
    if (-not $OutputPath) {
        $OutputPath = [Environment]::GetFolderPath("Desktop")
    }
    $ShortcutPath = Join-Path $OutputPath "$ShortcutName.lnk"
    
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = "$env:WINDIR\System32\vmconnect.exe"
    $Shortcut.Arguments = "localhost `"$VMName`""
    $Shortcut.Description = "Connect to $VMName"
    $Shortcut.IconLocation = "$env:WINDIR\System32\vmconnect.exe,0"
    $Shortcut.Save()
    
    Write-Host "VM connection shortcut created at: $ShortcutPath" -ForegroundColor Green
}

# Create shortcut that starts and connects to a VM
function Create-VMStartShortcut {
    param(
        [Parameter(Mandatory=$true)]
        [string]$VMName,
        [string]$ShortcutName = "Start $VMName",
        [string]$OutputPath = $null
    )
    
    if (-not $OutputPath) {
        $OutputPath = [Environment]::GetFolderPath("Desktop")
    }
    $ShortcutPath = Join-Path $OutputPath "$ShortcutName.lnk"
    
    # Create a PowerShell script that starts the VM and then connects
    $ScriptPath = Join-Path $env:TEMP "StartVM_$([System.IO.Path]::GetInvalidFileNameChars() -join '' | ForEach-Object { $VMName -replace $_, '_' }).ps1"
    $ScriptContent = @"
# Start VM and connect
# Import Hyper-V module
Import-Module Hyper-V -ErrorAction SilentlyContinue

`$vm = Get-VM -Name `"$VMName`" -ErrorAction SilentlyContinue
if (`$vm) {
    if (`$vm.State -ne 'Running') {
        Start-VM -Name `"$VMName`"
        Start-Sleep -Seconds 2
    }
    & `"$env:WINDIR\System32\vmconnect.exe`" localhost `"$VMName`"
} else {
    Write-Host `"VM '$VMName' not found!`" -ForegroundColor Red
    Write-Host `"Available VMs:`" -ForegroundColor Yellow
    Get-VM | Select-Object Name, State | Format-Table -AutoSize
    Read-Host `"Press Enter to exit`"
}
"@
    $ScriptContent | Out-File -FilePath $ScriptPath -Encoding UTF8
    
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = "powershell.exe"
    $Shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`""
    $Shortcut.Description = "Start and connect to $VMName"
    $Shortcut.IconLocation = "$env:WINDIR\System32\vmconnect.exe,0"
    $Shortcut.Save()
    
    Write-Host "VM start shortcut created at: $ShortcutPath" -ForegroundColor Green
    Write-Host "  (Script location: $ScriptPath)" -ForegroundColor Gray
}

# Main script logic
if ($ListVMs) {
    Get-VMList
    exit 0
}

if ($VMName) {
    # Verify VM exists (optional check, but helpful)
    try {
        Import-Module Hyper-V -ErrorAction SilentlyContinue
        $vm = Get-VM -Name $VMName -ErrorAction SilentlyContinue
        if (-not $vm) {
            Write-Host "Warning: VM '$VMName' not found!" -ForegroundColor Yellow
            Write-Host "Available VMs:" -ForegroundColor Cyan
            Get-VM | Select-Object Name, State | Format-Table -AutoSize
            Write-Host ""
            $continue = Read-Host "Continue anyway? (y/n)"
            if ($continue -ne 'y' -and $continue -ne 'Y') {
                exit 1
            }
        } else {
            Write-Host "Found VM: $VMName (State: $($vm.State))" -ForegroundColor Green
        }
    } catch {
        Write-Host "Note: Could not verify VM existence. Continuing..." -ForegroundColor Yellow
    }
    
    # If ShortcutName not provided, use VMName
    if (-not $ShortcutName) {
        if ($ConnectOnly) {
            $ShortcutName = $VMName
        } else {
            $ShortcutName = "Start $VMName"
        }
    }
    
    if ($ConnectOnly) {
        Create-VMConnectShortcut -VMName $VMName -ShortcutName $ShortcutName
    } else {
        Create-VMStartShortcut -VMName $VMName -ShortcutName $ShortcutName
    }
    exit 0
}

# Show usage if no parameters provided
Write-Host "Hyper-V Shortcut Creator" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "  .\create-hyperv-shortcut.ps1 <VMName> [ShortcutName] [-ConnectOnly]" -ForegroundColor White
Write-Host "  .\create-hyperv-shortcut.ps1 -ListVMs" -ForegroundColor White
Write-Host "  .\create-hyperv-shortcut.ps1 -CreateManager" -ForegroundColor White
Write-Host ""
Write-Host "Examples:" -ForegroundColor Yellow
Write-Host "  .\create-hyperv-shortcut.ps1 `"Windows 10`"" -ForegroundColor Gray
Write-Host "    Creates a shortcut that starts and connects to 'Windows 10' VM" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  .\create-hyperv-shortcut.ps1 `"Windows 10`" `"My VM`" -ConnectOnly" -ForegroundColor Gray
Write-Host "    Creates a shortcut named 'My VM' that only connects (doesn't start)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  .\create-hyperv-shortcut.ps1 -ListVMs" -ForegroundColor Gray
Write-Host "    Lists all available virtual machines" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  .\create-hyperv-shortcut.ps1 -CreateManager" -ForegroundColor Gray
Write-Host "    Creates a shortcut for Hyper-V Manager" -ForegroundColor DarkGray
Write-Host ""

