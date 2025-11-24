using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace QuickerExpressionAgent.Quicker.Services;

/// <summary>
/// Service for checking .NET runtime versions installed on the system
/// </summary>
public class DotNetVersionChecker
{
    private readonly ILogger<DotNetVersionChecker>? _logger;
    private const string RequiredVersion = "8.0";
    private const string DownloadUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.22-windows-x64-installer";

    public DotNetVersionChecker(ILogger<DotNetVersionChecker>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if .NET 8.0 Desktop Runtime is installed
    /// Note: .NET runtime versions are not forward compatible. A .NET 8.0 application requires .NET 8.0 runtime,
    /// and cannot run on .NET 10.0 runtime alone. We need to check for exactly 8.0 or higher, but the application
    /// specifically requires 8.0 runtime to run.
    /// </summary>
    /// <returns>True if .NET 8.0 Desktop Runtime is installed, false otherwise</returns>
    public bool IsDotNet80Installed()
    {
        try
        {
            // Method 1: Check via dotnet --list-runtimes command
            var installedVersions = GetInstalledRuntimes();
            if (installedVersions != null && installedVersions.Any())
            {
                foreach (var version in installedVersions)
                {
                    // Check if version is exactly 8.0 or starts with 8.0
                    // .NET 8.0 applications require 8.0 runtime, not just any 8.0+ version
                    if (IsVersion8(version))
                    {
                        _logger?.LogInformation("Found .NET 8.0 Desktop Runtime: {Version}", version);
                        return true;
                    }
                }
            }

            // Method 2: Check Windows Registry for .NET runtime installations
            var registryVersions = GetInstalledVersionsFromRegistry();
            if (registryVersions != null && registryVersions.Any())
            {
                foreach (var version in registryVersions)
                {
                    if (IsVersion8(version))
                    {
                        _logger?.LogInformation("Found .NET 8.0 Desktop Runtime in registry: {Version}", version);
                        return true;
                    }
                }
            }

            _logger?.LogWarning(".NET 8.0 Desktop Runtime not found");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking .NET runtime version");
            return false;
        }
    }

    /// <summary>
    /// Get the highest installed .NET runtime version
    /// </summary>
    /// <returns>Version string or null if not found</returns>
    public string? GetInstalledVersion()
    {
        try
        {
            // Method 1: Check via dotnet --list-runtimes command
            var installedVersions = GetInstalledRuntimes();
            if (installedVersions != null && installedVersions.Any())
            {
                var highestVersion = installedVersions
                    .Where(v => IsVersion8(v))
                    .OrderByDescending(v => ParseVersion(v))
                    .FirstOrDefault();
                
                if (highestVersion != null)
                {
                    return highestVersion;
                }
            }

            // Method 2: Check Windows Registry
            var registryVersions = GetInstalledVersionsFromRegistry();
            if (registryVersions != null && registryVersions.Any())
            {
                var highestVersion = registryVersions
                    .Where(v => IsVersion8(v))
                    .OrderByDescending(v => ParseVersion(v))
                    .FirstOrDefault();
                
                if (highestVersion != null)
                {
                    return highestVersion;
                }
            }

            // Return any installed version if 8.0+ not found
            var allVersions = installedVersions?.Concat(registryVersions ?? Enumerable.Empty<string>())
                .Distinct()
                .OrderByDescending(v => ParseVersion(v))
                .FirstOrDefault();
            
            return allVersions;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting installed .NET runtime version");
            return null;
        }
    }

    /// <summary>
    /// Get download URL for .NET 8.0 runtime
    /// </summary>
    public string GetDownloadUrl() => DownloadUrl;

    /// <summary>
    /// Get installed runtimes via dotnet --list-runtimes command
    /// </summary>
    private string[]? GetInstalledRuntimes()
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return null;
            }

            // Parse output: "Microsoft.WindowsDesktop.App 8.0.0 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]"
            // Desktop applications require Microsoft.WindowsDesktop.App, not just Microsoft.NETCore.App
            var versions = output.Split('\n')
                .Where(line => line.Contains("Microsoft.WindowsDesktop.App"))
                .Select(line =>
                {
                    var parts = line.Trim().Split(' ');
                    return parts.Length > 1 ? parts[1] : null;
                })
                .Where(v => v != null)
                .ToArray();

            return versions.Length > 0 ? versions : null;
        }
        catch
        {
            // dotnet command not found or other error
            return null;
        }
    }

    /// <summary>
    /// Get installed versions from Windows Registry
    /// </summary>
    private string[]? GetInstalledVersionsFromRegistry()
    {
        try
        {
            var versions = new List<string>();

            // Check HKEY_LOCAL_MACHINE\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\Version
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost");
            if (key != null)
            {
                var version = key.GetValue("Version") as string;
                if (!string.IsNullOrEmpty(version))
                {
                    versions.Add(version);
                }
            }

            // Check HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost\Version
            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost");
            if (key32 != null)
            {
                var version = key32.GetValue("Version") as string;
                if (!string.IsNullOrEmpty(version))
                {
                    versions.Add(version);
                }
            }

            // Check for .NET Desktop Runtime installations in registry (Desktop apps require Microsoft.WindowsDesktop.App)
            using var desktopRuntimeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App");
            if (desktopRuntimeKey != null)
            {
                var subKeys = desktopRuntimeKey.GetSubKeyNames();
                foreach (var subKeyName in subKeys)
                {
                    if (!string.IsNullOrEmpty(subKeyName))
                    {
                        versions.Add(subKeyName);
                    }
                }
            }

            // Also check WOW6432Node for 32-bit installations
            using var desktopRuntimeKey32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App");
            if (desktopRuntimeKey32 != null)
            {
                var subKeys = desktopRuntimeKey32.GetSubKeyNames();
                foreach (var subKeyName in subKeys)
                {
                    if (!string.IsNullOrEmpty(subKeyName))
                    {
                        versions.Add(subKeyName);
                    }
                }
            }

            return versions.Count > 0 ? versions.Distinct().ToArray() : null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading .NET versions from registry");
            return null;
        }
    }

    /// <summary>
    /// Check if version string represents .NET 8.0
    /// Note: .NET runtime versions are not forward compatible. A .NET 8.0 application requires .NET 8.0 runtime.
    /// We check for major version 8 (8.0, 8.1, 8.2, etc.) as these are compatible with .NET 8.0 applications.
    /// </summary>
    private bool IsVersion8(string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }

        try
        {
            var parsedVersion = ParseVersion(version);
            if (parsedVersion == null)
            {
                return false;
            }

            // Check if major version is exactly 8 (8.0, 8.1, 8.2, etc. are all compatible)
            // .NET 10.0 runtime cannot run .NET 8.0 applications
            return parsedVersion.Major == 8;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parse version string to Version object
    /// </summary>
    private Version? ParseVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return null;
        }

        // Remove any trailing characters after version numbers
        var cleanVersion = version.Split('-', '+', ' ')[0];
        
        if (Version.TryParse(cleanVersion, out var parsedVersion))
        {
            return parsedVersion;
        }

        return null;
    }
}

