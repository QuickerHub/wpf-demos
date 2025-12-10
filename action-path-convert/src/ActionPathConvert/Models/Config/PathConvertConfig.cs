using CommunityToolkit.Mvvm.ComponentModel;

namespace ActionPathConvert.Models.Config
{
    /// <summary>
    /// Path conversion configuration
    /// </summary>
    public partial class PathConvertConfig : ObservableObject
    {
        [ObservableProperty]
        private string _searchDirectory = "";

        [ObservableProperty]
        private string _audioExtensions = "*.mp3,*.flac,*.mp4,*.wav,*.m4a,*.aac,*.ogg,*.wma";

        [ObservableProperty]
        private string _preferredExtension = ".mp3";

        [ObservableProperty]
        private bool _useRelativePath = true;
    }
}

