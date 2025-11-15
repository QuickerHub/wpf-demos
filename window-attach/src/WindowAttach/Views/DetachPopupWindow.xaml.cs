using System;
using System.Windows;
using System.Windows.Interop;
using log4net;
using WindowAttach;
using WindowAttach.Models;
using WindowAttach.Services;
using WindowAttach.Utils;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowAttach.Views
{
    /// <summary>
    /// Popup window for detaching window attachment
    /// </summary>
    public partial class DetachPopupWindow : Window
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DetachPopupWindow));
        
        public IntPtr Window1Handle { get; set; }
        public IntPtr Window2Handle { get; set; }
        private Action? _callbackAction;

        public DetachPopupWindow(IntPtr window1Handle, IntPtr window2Handle, Action? callbackAction = null)
        {
            InitializeComponent();
            Window1Handle = window1Handle;
            Window2Handle = window2Handle;
            _callbackAction = callbackAction;
            
            // Set initial position - will be updated by attachment service
            WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Prevent window from getting focus
            ShowActivated = false;
            Focusable = false;
            
            // Hide callback button (for debugging purposes, can be enabled if needed)
            if (CallbackButton != null)
            {
                CallbackButton.Visibility = Visibility.Collapsed;
            }
            
            // Load current settings
            LoadCurrentSettings();

            Width = 0;
            Height = 0;
        }
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Set WS_EX_NOACTIVATE immediately after window creation, BEFORE window is shown
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                // Set WS_EX_NOACTIVATE to prevent window from getting focus
                WindowHelper.SetWindowExStyle(hwnd, WINDOW_EX_STYLE.WS_EX_NOACTIVATE, true);
            }
        }
        
        private void LoadCurrentSettings()
        {
            var managerService = AppState.ManagerService;
            var key = $"{Window1Handle}_{Window2Handle}_{AttachType.Main}";
            var pair = managerService.GetPair(key);
            
            if (pair != null)
            {
                SettingsSelector.SetSettings(pair.RestrictToSameScreen, pair.AutoAdjustToScreen);
            }
        }

        private void DetachButton_Click(object sender, RoutedEventArgs e)
        {
            // Unregister the main attachment (this will also destroy this popup)
            Runner.Unregister(Window1Handle, Window2Handle);
            Close();
        }

        private void PlacementSelector_PlacementSelected(object? sender, WindowPlacement placement)
        {
            // Update the placement for the main attachment
            var managerService = AppState.ManagerService;
            managerService.UpdatePlacement(Window1Handle, Window2Handle, placement);
            
            // Close the popup
            PlacementButton.IsChecked = false;
        }
        
        private void SettingsSelector_SettingsChanged(bool restrictToSameScreen, bool autoAdjustToScreen)
        {
            // Update the settings for the main attachment
            var managerService = AppState.ManagerService;
            managerService.UpdateSettings(Window1Handle, Window2Handle, restrictToSameScreen, autoAdjustToScreen);
        }

        private void CallbackButton_Click(object sender, RoutedEventArgs e)
        {
            // Execute callback action if provided
            if (_callbackAction != null)
            {
                try
                {
                    _log.Info("Executing callback action");
                    _callbackAction.Invoke();
                    _log.Info("Callback action executed successfully");
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    _log.Error("Error executing callback action", ex);
                }
            }
            else
            {
                _log.Warn("Callback action is null, cannot execute");
            }
        }
    }
}

