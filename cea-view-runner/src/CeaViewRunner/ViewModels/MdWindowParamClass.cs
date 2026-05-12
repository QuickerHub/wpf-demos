using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CeaViewRunner.ViewModels;

[ObservableObject]
public partial class MdWindowParamClass : ParamClass
{
    private double _maxWidth = int.MaxValue;

    public double maxWidth
    {
        get => _maxWidth;
        set => SetProperty(ref _maxWidth, value, nameof(maxWidth));
    }

    private double? _width = 300;

    public double? width
    {
        get => _width;
        set => SetProperty(ref _width, value, nameof(width));
    }

    private double _maxHeight = int.MaxValue;

    public double maxHeight
    {
        get => _maxHeight;
        set => SetProperty(ref _maxHeight, value, nameof(maxHeight));
    }

    private double? _height;

    public double? height
    {
        get => _height;
        set
        {
            if (SetProperty(ref _height, value, nameof(height)))
            {
                OnPropertyChanged(nameof(sizeToContent));
            }
        }
    }

    private Brush? _background;

    public Brush? background
    {
        get => _background;
        set => SetProperty(ref _background, value, nameof(background));
    }

    private string? _backgroundImage = "";

    public string? backgroundImage
    {
        get => _backgroundImage;
        set => SetProperty(ref _backgroundImage, value, nameof(backgroundImage));
    }

    private double? _opacity = 1;

    public double? opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value, nameof(opacity));
    }

    private Thickness? _padding = new(15, 10, 15, 10);

    public Thickness? padding
    {
        get => _padding;
        set => SetProperty(ref _padding, value, nameof(padding));
    }

    public SizeToContent sizeToContent => height == null ? SizeToContent.Height : SizeToContent.Manual;
}
