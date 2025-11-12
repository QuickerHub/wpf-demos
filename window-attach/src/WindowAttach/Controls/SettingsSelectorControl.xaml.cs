using System;
using System.Windows;
using System.Windows.Controls;

namespace WindowAttach.Controls
{
    /// <summary>
    /// Control for selecting attachment settings with checkboxes
    /// </summary>
    public partial class SettingsSelectorControl : UserControl
    {
        /// <summary>
        /// Event raised when settings change
        /// </summary>
        public event Action<bool, bool>? SettingsChanged;

        public SettingsSelectorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the current settings values
        /// </summary>
        public void SetSettings(bool restrictToSameScreen, bool autoAdjustToScreen)
        {
            RestrictToSameScreenCheckBox.IsChecked = restrictToSameScreen;
            AutoAdjustToScreenCheckBox.IsChecked = autoAdjustToScreen;
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool restrictToSameScreen = RestrictToSameScreenCheckBox.IsChecked ?? false;
            bool autoAdjustToScreen = AutoAdjustToScreenCheckBox.IsChecked ?? false;
            SettingsChanged?.Invoke(restrictToSameScreen, autoAdjustToScreen);
        }
    }
}

