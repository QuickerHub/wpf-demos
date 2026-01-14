using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QuickerScreenshot.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string Title { get; set; } = "Quicker Screenshot";

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [RelayCommand]
        private void TakeScreenshot()
        {
            // TODO: Implement screenshot functionality
        }
    }
}
