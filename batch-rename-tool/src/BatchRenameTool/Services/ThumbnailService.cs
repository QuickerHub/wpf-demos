using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BatchRenameTool.Services;

/// <summary>
/// Service for getting file thumbnails using Windows Shell API
/// </summary>
public class ThumbnailService
{
    private class ThumbnailInfo
    {
        public BitmapSource? Thumbnail { get; set; }
        public DateTime LastAccessed { get; set; }

        public ThumbnailInfo(BitmapSource? thumbnail)
        {
            Thumbnail = thumbnail;
            LastAccessed = DateTime.Now;
        }
    }

    // Cache with path as key and thumbnail info as value
    private readonly ConcurrentDictionary<string, ThumbnailInfo> _cache = new();

    // Maximum number of thumbnails to keep in cache
    private readonly int _maxCacheSize;

    public ThumbnailService(int maxCacheSize = 200)
    {
        _maxCacheSize = maxCacheSize;
    }

    /// <summary>
    /// Get a thumbnail for the file at the specified path (synchronous, checks cache only)
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="maxHeight">Maximum height of the thumbnail</param>
    /// <returns>Bitmap thumbnail or null if file cannot be loaded</returns>
    public BitmapSource? GetThumbnail(string filePath, int maxHeight = 100)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var cacheKey = $"{filePath}_{maxHeight}";

        // Check if thumbnail is already in cache
        if (_cache.TryGetValue(cacheKey, out var info))
        {
            // Update last accessed time
            info.LastAccessed = DateTime.Now;
            return info.Thumbnail;
        }

        return null; // Not in cache, use async method to load
    }

    /// <summary>
    /// Get a thumbnail for the file at the specified path asynchronously
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="maxHeight">Maximum height of the thumbnail</param>
    /// <returns>Bitmap thumbnail or null if file cannot be loaded</returns>
    public async Task<BitmapSource?> GetThumbnailAsync(string filePath, int maxHeight = 100)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var cacheKey = $"{filePath}_{maxHeight}";

        // Check if thumbnail is already in cache
        if (_cache.TryGetValue(cacheKey, out var info))
        {
            // Update last accessed time
            info.LastAccessed = DateTime.Now;
            return info.Thumbnail;
        }

        // Create new thumbnail asynchronously
        var thumbnail = await Task.Run(() => CreateThumbnail(filePath, maxHeight));
        if (thumbnail == null)
            return null;

        // Add to cache
        _cache[cacheKey] = new ThumbnailInfo(thumbnail);

        // Clean cache if necessary
        CleanCacheIfNeeded();

        return thumbnail;
    }

    /// <summary>
    /// Clear all cached thumbnails
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        GC.Collect();
    }

    /// <summary>
    /// Remove a specific thumbnail from the cache
    /// </summary>
    /// <param name="filePath">Path of the file whose thumbnail should be removed</param>
    public void RemoveFromCache(string filePath)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(filePath + "_")).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private BitmapSource? CreateThumbnail(string filePath, int maxHeight)
    {
        try
        {
            // Try to get thumbnail using Windows Shell API
            var thumbnail = GetShellThumbnail(filePath, maxHeight);
            if (thumbnail != null)
                return thumbnail;

            // Fallback: For image files, use BitmapImage
            if (IsImageFile(filePath))
            {
                return GetImageThumbnail(filePath, maxHeight);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private BitmapSource? GetShellThumbnail(string filePath, int maxHeight)
    {
        object? shellItemObj = null;
        try
        {
            // Use IShellItemImageFactory to get thumbnail
            var hr = NativeMethods.SHCreateItemFromParsingName(filePath, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out shellItemObj);
            if (hr != 0 || shellItemObj == null) // S_OK = 0
                return null;

            var imageFactory = (IShellItemImageFactory)shellItemObj;
            var size = new NativeMethods.SIZE { cx = maxHeight * 2, cy = maxHeight * 2 }; // Request larger size for better quality
            var flags = NativeMethods.SIIGBF.SIIGBF_THUMBNAILONLY | NativeMethods.SIIGBF.SIIGBF_BIGGERSIZEOK;

            var hBitmap = IntPtr.Zero;
            hr = imageFactory.GetImage(size, flags, out hBitmap);

            if (hr != 0 || hBitmap == IntPtr.Zero)
                return null;

            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(maxHeight, maxHeight));

                if (bitmapSource.CanFreeze)
                    bitmapSource.Freeze();

                return bitmapSource;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(hBitmap);
                }
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (shellItemObj != null)
            {
                Marshal.ReleaseComObject(shellItemObj);
            }
        }
    }

    private BitmapSource? GetImageThumbnail(string filePath, int maxHeight)
    {
        try
        {
            // Get original dimensions first
            var frame = BitmapFrame.Create(new Uri(filePath), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            int originalHeight = frame.PixelHeight;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath);

            // Only set decode height if image is larger than max height
            if (originalHeight > maxHeight)
            {
                bitmap.DecodePixelHeight = maxHeight;
            }

            bitmap.EndInit();
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif" || ext == ".webp";
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

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c46b4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage([In, MarshalAs(UnmanagedType.Struct)] NativeMethods.SIZE size, [In] NativeMethods.SIIGBF flags, out IntPtr phbm);
    }

    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppv);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00000000,
            SIIGBF_BIGGERSIZEOK = 0x00000001,
            SIIGBF_MEMORYONLY = 0x00000002,
            SIIGBF_ICONONLY = 0x00000004,
            SIIGBF_THUMBNAILONLY = 0x00000008,
            SIIGBF_INCACHEONLY = 0x00000010,
            SIIGBF_CROPTOSQUARE = 0x00000020,
            SIIGBF_WIDETHUMBNAILS = 0x00000040,
            SIIGBF_ICONBACKGROUND = 0x00000080,
            SIIGBF_SCALEUP = 0x00000100
        }
    }

    #endregion
}

