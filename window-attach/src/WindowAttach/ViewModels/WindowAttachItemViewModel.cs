using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowAttach.Services;
using WindowAttach.Utils;
using WindowAttach;

namespace WindowAttach.ViewModels
{
    /// <summary>
    /// ViewModel for a single window attachment item
    /// </summary>
    public partial class WindowAttachItemViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial IntPtr Window1Handle { get; set; }

        [ObservableProperty]
        public partial IntPtr Window2Handle { get; set; }

        [ObservableProperty]
        public partial string Window1Title { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Window2Title { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Window1HandleText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string Window2HandleText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial DateTime RegisteredTime { get; set; }

        public WindowAttachItemViewModel(IntPtr window1Handle, IntPtr window2Handle, DateTime registeredTime = default)
        {
            Window1Handle = window1Handle;
            Window2Handle = window2Handle;
            RegisteredTime = registeredTime == default ? DateTime.Now : registeredTime;

            UpdateWindowInfo();
        }

        private void UpdateWindowInfo()
        {
            Window1Title = WindowHelper.GetWindowText(Window1Handle);
            Window2Title = WindowHelper.GetWindowText(Window2Handle);
            Window1HandleText = $"0x{Window1Handle.ToInt64():X}";
            Window2HandleText = $"0x{Window2Handle.ToInt64():X}";

            if (string.IsNullOrEmpty(Window1Title))
                Window1Title = "(无标题)";
            if (string.IsNullOrEmpty(Window2Title))
                Window2Title = "(无标题)";
        }

        [RelayCommand]
        private void Detach()
        {
            Runner.Unregister(Window1Handle, Window2Handle);
            // Notify parent to refresh list
            Detached?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Detached;
    }
}

