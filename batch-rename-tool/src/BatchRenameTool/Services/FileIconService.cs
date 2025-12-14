using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BatchRenameTool.Services;

/// <summary>
/// Service for getting file icons using Windows Shell API
/// </summary>
public class FileIconService
{
    private class IconInfo
    {
        public BitmapSource? Icon { get; set; }
        public DateTime LastAccessed { get; set; }

        public IconInfo(BitmapSource? icon)
        {
            Icon = icon;
            LastAccessed = DateTime.Now;
        }
    }

    private class IconLoadRequest
    {
        public string FilePath { get; set; } = "";
        public int Size { get; set; }
        public TaskCompletionSource<BitmapSource?> CompletionSource { get; set; } = new();
    }

    // Cache with path as key and icon info as value
    private readonly ConcurrentDictionary<string, IconInfo> _cache = new();

    // Queue for icon loading requests
    private readonly ConcurrentQueue<IconLoadRequest> _loadQueue = new();

    // Semaphore to limit concurrent icon loading
    private readonly SemaphoreSlim _loadSemaphore;

    // Maximum number of icons to keep in cache
    private readonly int _maxCacheSize;
    private readonly int _iconSize;
    private readonly int _maxConcurrentLoads;

    // Background task to process the queue
    private readonly Task _queueProcessorTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public FileIconService(int maxCacheSize = 500, int iconSize = 16, int maxConcurrentLoads = 5)
    {
        _maxCacheSize = maxCacheSize;
        _iconSize = iconSize;
        _maxConcurrentLoads = maxConcurrentLoads;
        _loadSemaphore = new SemaphoreSlim(maxConcurrentLoads, maxConcurrentLoads);
        
        // Start background task to process the queue
        _queueProcessorTask = Task.Run(ProcessLoadQueueAsync, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Get an icon for the file at the specified path (synchronous, checks cache only)
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="size">Size of the icon</param>
    /// <returns>Bitmap icon or null if file cannot be loaded</returns>
    public BitmapSource? GetIcon(string filePath, int? size = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var iconSize = size ?? _iconSize;
        var cacheKey = $"{filePath}_{iconSize}";

        // Check if icon is already in cache
        if (_cache.TryGetValue(cacheKey, out var info))
        {
            // Update last accessed time
            info.LastAccessed = DateTime.Now;
            return info.Icon;
        }

        return null; // Not in cache, use async method to load
    }

    /// <summary>
    /// Get an icon for the file at the specified path asynchronously
    /// Uses a queue to prevent high concurrency
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="size">Size of the icon</param>
    /// <returns>Bitmap icon or null if file cannot be loaded</returns>
    public async Task<BitmapSource?> GetIconAsync(string filePath, int? size = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var iconSize = size ?? _iconSize;
        var cacheKey = $"{filePath}_{iconSize}";

        // Check if icon is already in cache
        if (_cache.TryGetValue(cacheKey, out var info))
        {
            // Update last accessed time
            info.LastAccessed = DateTime.Now;
            return info.Icon;
        }

        // Create a request and add to queue
        var request = new IconLoadRequest
        {
            FilePath = filePath,
            Size = iconSize
        };

        _loadQueue.Enqueue(request);

        // Wait for the icon to be loaded
        return await request.CompletionSource.Task;
    }

    /// <summary>
    /// Background task to process icon loading queue
    /// </summary>
    private async Task ProcessLoadQueueAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Try to dequeue a request
                if (_loadQueue.TryDequeue(out var request))
                {
                    // Wait for semaphore slot
                    await _loadSemaphore.WaitAsync(_cancellationTokenSource.Token);

                    // Process the request in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var cacheKey = $"{request.FilePath}_{request.Size}";

                            // Double-check cache (might have been loaded by another request)
                            if (_cache.TryGetValue(cacheKey, out var cachedInfo))
                            {
                                request.CompletionSource.SetResult(cachedInfo.Icon);
                                return;
                            }

                            // Create icon
                            var icon = CreateIcon(request.FilePath, request.Size);
                            
                            if (icon != null)
                            {
                                // Add to cache
                                _cache[cacheKey] = new IconInfo(icon);
                                CleanCacheIfNeeded();
                            }

                            // Set result
                            request.CompletionSource.SetResult(icon);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing icon load request for {request.FilePath}: {ex.Message}");
                            request.CompletionSource.SetResult(null);
                        }
                        finally
                        {
                            _loadSemaphore.Release();
                        }
                    }, _cancellationTokenSource.Token);
                }
                else
                {
                    // No items in queue, wait a bit before checking again
                    await Task.Delay(10, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in queue processor: {ex.Message}");
                await Task.Delay(100, _cancellationTokenSource.Token);
            }
        }
    }

    /// <summary>
    /// Clear all cached icons
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        GC.Collect();
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _queueProcessorTask?.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
        _loadSemaphore.Dispose();
    }

    /// <summary>
    /// Remove a specific icon from the cache
    /// </summary>
    /// <param name="filePath">Path of the file whose icon should be removed</param>
    public void RemoveFromCache(string filePath)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(filePath + "_")).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private BitmapSource? CreateIcon(string filePath, int size)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            // Verify file still exists
            if (!File.Exists(filePath))
                return null;

            // Use SHGetFileInfo to get file icon
            var shfi = new NativeMethods.SHFILEINFO();
            
            // Use appropriate icon size flag
            uint flags = NativeMethods.SHGFI_ICON;
            if (size <= 16)
            {
                flags |= NativeMethods.SHGFI_SMALLICON;
            }
            else
            {
                flags |= NativeMethods.SHGFI_LARGEICON;
            }
            
            // Get file attributes
            uint fileAttributes = (uint)File.GetAttributes(filePath);
            
            var result = NativeMethods.SHGetFileInfo(
                filePath,
                fileAttributes,
                ref shfi,
                (uint)Marshal.SizeOf(typeof(NativeMethods.SHFILEINFO)),
                flags);

            // SHGetFileInfo returns non-zero on success
            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"SHGetFileInfo failed for {filePath}");
                return null;
            }

            hIcon = shfi.hIcon;

            // Convert icon to BitmapSource
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(size, size));

            if (bitmapSource.CanFreeze)
                bitmapSource.Freeze();

            return bitmapSource;
        }
        catch (Exception ex)
        {
            // Log exception for debugging
            System.Diagnostics.Debug.WriteLine($"Error creating icon for {filePath}: {ex.Message}");
            return null;
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(hIcon);
            }
        }
    }

    private void CleanCacheIfNeeded()
    {
        if (_cache.Count <= _maxCacheSize)
            return;

        // Get list of items sorted by last accessed time (oldest first)
        var oldestItems = _cache
            .OrderBy(x => x.Value.LastAccessed)
            .Take(_cache.Count - _maxCacheSize)
            .ToList();

        // Remove oldest items
        foreach (var item in oldestItems)
        {
            _cache.TryRemove(item.Key, out _);
        }
    }

    #region Native Methods

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_SMALLICON = 0x000000001;
        public const uint SHGFI_LARGEICON = 0x000000000;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
    }

    #endregion
}

