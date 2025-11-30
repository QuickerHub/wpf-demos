using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsTools.Services;

namespace WindowsTools.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsDesktopIconsVisible { get; set; }

        public MainWindowViewModel()
        {
            // Initialize desktop icons visibility status
            UpdateDesktopIconsStatus();
        }

        /// <summary>
        /// Update desktop icons visibility status
        /// </summary>
        public void UpdateDesktopIconsStatus()
        {
            IsDesktopIconsVisible = DesktopIconsService.IsVisible();
        }

        /// <summary>
        /// Toggle desktop icons visibility
        /// </summary>
        [RelayCommand]
        private void ToggleDesktopIcons()
        {
            DesktopIconsService.Toggle();
            // Update status after a short delay to allow Windows to process the change
            System.Threading.Thread.Sleep(100);
            UpdateDesktopIconsStatus();
        }

        /// <summary>
        /// Show desktop icons
        /// </summary>
        [RelayCommand]
        private void ShowDesktopIcons()
        {
            DesktopIconsService.Show();
            System.Threading.Thread.Sleep(100);
            UpdateDesktopIconsStatus();
        }

        /// <summary>
        /// Hide desktop icons
        /// </summary>
        [RelayCommand]
        private void HideDesktopIcons()
        {
            DesktopIconsService.Hide();
            System.Threading.Thread.Sleep(100);
            UpdateDesktopIconsStatus();
        }
    }
}

