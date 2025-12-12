using Quicker.Utilities;
using System.Windows;

namespace BatchRenameTool
{
    /// <summary>
    /// Helper for showing messages using Quicker's AppHelper
    /// </summary>
    public static class MessageHelper
    {
        /// <summary>
        /// Show information message
        /// </summary>
        public static void ShowInformation(string message)
        {
            try
            {
                AppHelper.ShowInformation(message);
            }
            catch
            {
                // Fallback to MessageBox if Quicker is not available
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Show warning message
        /// </summary>
        public static void ShowWarning(string message)
        {
            try
            {
                AppHelper.ShowWarning(message);
            }
            catch
            {
                // Fallback to MessageBox if Quicker is not available
                MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Show success message
        /// </summary>
        public static void ShowSuccess(string message)
        {
            try
            {
                AppHelper.ShowSuccess(message);
            }
            catch
            {
                // Fallback to MessageBox if Quicker is not available
                MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Show error message
        /// </summary>
        public static void ShowError(string message)
        {
            try
            {
                AppHelper.ShowError(message);
            }
            catch
            {
                // Fallback to MessageBox if Quicker is not available
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
