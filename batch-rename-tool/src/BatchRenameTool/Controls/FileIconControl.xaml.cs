using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BatchRenameTool.Services;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls;

/// <summary>
/// Control for displaying file icons
/// </summary>
[DependencyProperty<string>("FilePath", DefaultValue = "")]
[DependencyProperty<int>("IconSize", DefaultValue = 16)]
public partial class FileIconControl : UserControl
{
    private static readonly FileIconService _iconService = new(maxCacheSize: 500, iconSize: 16, maxConcurrentLoads: 1);
    private string? _currentFilePath;
    private Task? _currentLoadTask;

    public FileIconControl()
    {
        InitializeComponent();
        // Don't set DataContext = this, as it will override the parent DataContext
        // Instead, bind to properties using RelativeSource
    }

    partial void OnFilePathChanged(string newValue)
    {
        UpdateIconAsync();
    }

    partial void OnIconSizeChanged(int newValue)
    {
        UpdateIconAsync();
    }

    private void UpdateIconAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            IconImage.Source = null;
            _currentFilePath = null;
            return;
        }

        if (!File.Exists(FilePath))
        {
            IconImage.Source = null;
            _currentFilePath = null;
            return;
        }

        // Store current file path to check if it's still valid when async operation completes
        var requestedPath = FilePath;
        _currentFilePath = requestedPath;

        // Try to get from cache first (synchronous, fast)
        var cachedIcon = _iconService.GetIcon(requestedPath, IconSize);
        if (cachedIcon != null)
        {
            IconImage.Source = cachedIcon;
            return;
        }

        // Load asynchronously - don't await, fire and forget
        _ = LoadIconAsync(requestedPath, IconSize);
    }

    private async Task LoadIconAsync(string filePath, int iconSize)
    {
        try
        {
            // Load icon in background thread
            var icon = await _iconService.GetIconAsync(filePath, iconSize).ConfigureAwait(false);

            // Check if file path hasn't changed while loading
            if (_currentFilePath != filePath)
            {
                // File path changed, ignore this result
                return;
            }

            // Update UI on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                // Double-check file path is still valid and matches
                if (_currentFilePath == filePath && FilePath == filePath)
                {
                    IconImage.Source = icon;
                }
            });
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Error loading icon for {filePath}: {ex.Message}");
            
            // On error, only update UI if file path hasn't changed
            if (_currentFilePath == filePath)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (_currentFilePath == filePath && FilePath == filePath)
                    {
                        // Keep current icon or set to null - don't clear if path still matches
                        // IconImage.Source = null;
                    }
                });
            }
        }
    }
}

