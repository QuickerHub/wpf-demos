using System.Windows;
using System.Windows.Media;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Helper class to detect HandyControl theme (light or dark)
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Get current HandyControl theme as string ("light" or "dark")
        /// Detects theme by checking background color brightness
        /// </summary>
        public static string GetCurrentTheme()
        {
            try
            {
                var resources = Application.Current.Resources;
                
                // Try to get background color from HandyControl theme resources
                if (resources.Contains("DefaultWindowBackgroundBrush"))
                {
                    var brush = resources["DefaultWindowBackgroundBrush"] as SolidColorBrush;
                    if (brush != null)
                    {
                        var color = brush.Color;
                        // Calculate brightness (luminance) using standard formula
                        var brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255.0;
                        // If brightness is less than 0.5, it's likely a dark theme
                        return brightness < 0.5 ? "dark" : "light";
                    }
                }
                
                // Fallback: Try other common HandyControl theme brushes
                if (resources.Contains("RegionBrush"))
                {
                    var brush = resources["RegionBrush"] as SolidColorBrush;
                    if (brush != null)
                    {
                        var color = brush.Color;
                        var brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255.0;
                        return brightness < 0.5 ? "dark" : "light";
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            // Default to light theme if detection fails
            return "light";
        }

        /// <summary>
        /// Check if current theme is dark
        /// </summary>
        public static bool IsDarkTheme()
        {
            return GetCurrentTheme() == "dark";
        }
    }
}

