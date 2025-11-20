using System;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Startup configuration for the application
/// </summary>
public partial class StartupConfig : ObservableObject
{
    [ObservableProperty]
    private string? _programFolder;

    /// <summary>
    /// Get the startup executable path by searching for exe files in the specified directory
    /// </summary>
    public string? GetStartupPath()
    {
        // If ProgramFolder is not set, return null
        if (string.IsNullOrWhiteSpace(ProgramFolder))
        {
            return null;
        }

        // Check if the directory exists
        if (!Directory.Exists(ProgramFolder))
        {
            return null;
        }

        // Search for exe files in the specified directory (current directory only)
        try
        {
            var exeFiles = Directory.GetFiles(ProgramFolder, "*.exe", SearchOption.TopDirectoryOnly);
            
            // Prefer QuickerExpressionAgent.exe if it exists
            var preferredExe = exeFiles.FirstOrDefault(f => 
                Path.GetFileName(f).Equals("QuickerExpressionAgent.exe", System.StringComparison.OrdinalIgnoreCase));
            
            if (preferredExe != null && File.Exists(preferredExe))
            {
                return preferredExe;
            }

            // If preferred exe not found, return the first exe file found
            var firstExe = exeFiles.FirstOrDefault();
            if (firstExe != null && File.Exists(firstExe))
            {
                return firstExe;
            }
        }
        catch (Exception)
        {
            // If there's an error accessing the directory, return null
            return null;
        }

        return null;
    }
}

