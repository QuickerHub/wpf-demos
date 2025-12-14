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
/// Control for displaying file thumbnails
/// </summary>
[DependencyProperty<string>("FilePath", DefaultValue = "")]
[DependencyProperty<int>("ThumbnailSize", DefaultValue = 100)]
public partial class FileThumbnailControl : UserControl
{
    private static readonly ThumbnailService _thumbnailService = new();
    private string? _currentFilePath;
    private Task? _currentLoadTask;

    public FileThumbnailControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    partial void OnFilePathChanged(string newValue)
    {
        UpdateThumbnailAsync();
    }

    partial void OnThumbnailSizeChanged(int newValue)
    {
        UpdateThumbnailAsync();
    }

    private void UpdateThumbnailAsync()
    {
        // Clear current thumbnail first
        ThumbnailImage.Source = null;

        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
        {
            return;
        }

        // Store current file path to check if it's still valid when async operation completes
        _currentFilePath = FilePath;

        // Cancel previous load task if any
        if (_currentLoadTask != null && !_currentLoadTask.IsCompleted)
        {
            // Note: We can't cancel the task, but we can check if the file path changed
        }

        // Try to get from cache first (synchronous, fast)
        var cachedThumbnail = _thumbnailService.GetThumbnail(FilePath, ThumbnailSize);
        if (cachedThumbnail != null)
        {
            ThumbnailImage.Source = cachedThumbnail;
            return;
        }

        // Load asynchronously
        _currentLoadTask = LoadThumbnailAsync(FilePath, ThumbnailSize);
    }

    private async Task LoadThumbnailAsync(string filePath, int thumbnailSize)
    {
        try
        {
            // Load thumbnail in background thread
            var thumbnail = await _thumbnailService.GetThumbnailAsync(filePath, thumbnailSize);

            // Check if file path hasn't changed while loading
            if (_currentFilePath != filePath)
            {
                // File path changed, ignore this result
                return;
            }

            // Update UI on UI thread
            Dispatcher.Invoke(() =>
            {
                // Double-check file path is still valid
                if (_currentFilePath == filePath)
                {
                    ThumbnailImage.Source = thumbnail;
                }
            });
        }
        catch
        {
            // On error, only update UI if file path hasn't changed
            if (_currentFilePath == filePath)
            {
                Dispatcher.Invoke(() =>
                {
                    if (_currentFilePath == filePath)
                    {
                        ThumbnailImage.Source = null;
                    }
                });
            }
        }
    }
}

