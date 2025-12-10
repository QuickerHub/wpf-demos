using Quicker.Utilities;

namespace ActionPathConvert
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
                // Ignore errors if Quicker is not available
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
                // Ignore errors if Quicker is not available
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
                // Ignore errors if Quicker is not available
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
                // Ignore errors if Quicker is not available
            }
        }
    }
}

