using System;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for Info page
/// </summary>
public partial class InfoPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _version = GetVersion();

    [ObservableProperty]
    private string _fileVersion = GetFileVersion();

    [ObservableProperty]
    private string _assemblyVersion = GetAssemblyVersion();

    [ObservableProperty]
    private string _productName = GetProductName();

    [ObservableProperty]
    private string _companyName = GetCompanyName();

    [ObservableProperty]
    private string _copyright = GetCopyright();

    [ObservableProperty]
    private string _description = GetDescription();

    private static string GetVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.FileVersion ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetFileVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.FileVersion ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetAssemblyVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetProductName()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.ProductName ?? "Quicker Expression Agent";
        }
        catch
        {
            return "Quicker Expression Agent";
        }
    }

    private static string GetCompanyName()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.CompanyName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetCopyright()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.LegalCopyright ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetDescription()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return versionInfo.FileDescription ?? "Quicker Expression Agent Desktop";
        }
        catch
        {
            return "Quicker Expression Agent Desktop";
        }
    }
}

