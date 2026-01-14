using Quicker.Utilities;
using System.Reflection;
using System.Windows;

namespace BatchRenameTool
{
    /// <summary>
    /// Helper for showing messages using Quicker's AppHelper
    /// </summary>
    public static class MessageHelper
    {
        private static readonly bool IsInQuicker;

        static MessageHelper()
        {
            // Check if running in Quicker by checking the entry assembly name
            IsInQuicker = Assembly.GetEntryAssembly()?.GetName().Name == "Quicker";
        }

        /// <summary>
        /// Show information message
        /// </summary>
        public static void ShowInformation(string message)
        {
            if (IsInQuicker)
            {
                AppHelper.ShowInformation(message);
            }
            else
            {
                // Fallback to MessageBox if not running in Quicker
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Show warning message
        /// </summary>
        public static void ShowWarning(string message)
        {
            if (IsInQuicker)
            {
                AppHelper.ShowWarning(message);
            }
            else
            {
                // Fallback to MessageBox if not running in Quicker
                MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Show success message
        /// </summary>
        public static void ShowSuccess(string message)
        {
            if (IsInQuicker)
            {
                AppHelper.ShowSuccess(message);
            }
            else
            {
                // Fallback to MessageBox if not running in Quicker
                MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Show error message
        /// </summary>
        public static void ShowError(string message)
        {
            if (IsInQuicker)
            {
                AppHelper.ShowError(message);
            }
            else
            {
                // Fallback to MessageBox if not running in Quicker
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
