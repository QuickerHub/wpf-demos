using System;
using System.IO;
#if WPF
using System.Windows.Media.Imaging;
#endif

namespace BatchRenameTool.Template.Evaluator;

/// <summary>
/// Image information with lazy loading
/// </summary>
public class ImageInfo : IImageInfo
{
    private readonly string _filePath;
    private readonly Lazy<(int Width, int Height)> _dimensions;

    public int Width => _dimensions.Value.Width;
    public int Height => _dimensions.Value.Height;

    public ImageInfo(string filePath)
    {
        _filePath = filePath;
        _dimensions = new Lazy<(int Width, int Height)>(
            () => LoadImageDimensions(filePath),
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private static (int Width, int Height) LoadImageDimensions(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return (0, 0);
        }

#if WPF
        try
        {
            using var stream = File.OpenRead(filePath);
            var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
#else
        // In non-WPF projects, return (0, 0) as image dimensions are not available
        return (0, 0);
#endif
    }
}
