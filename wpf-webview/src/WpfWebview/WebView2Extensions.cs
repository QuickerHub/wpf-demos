using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace WpfWebview
{
    /// <summary>
    /// Extension methods for WebView2 control
    /// </summary>
    public static class WebView2Extensions
    {
        /// <summary>
        /// Set default background color from HandyControl theme brush
        /// </summary>
        /// <param name="webView">WebView2 control</param>
        /// <param name="resources">Resource dictionary containing HandyControl theme brushes</param>
        public static void SetBackgroundColorForHandyControl(this WebView2 webView, ResourceDictionary resources)
        {
            var brush = resources["DefaultWindowBackgroundBrush"] as Brush;
            webView.SetBackgroundColor(brush ?? Brushes.White);
        }
        /// <summary>
        /// Set default background color from a WPF brush
        /// </summary>
        /// <param name="webView">WebView2 control</param>
        /// <param name="brush">WPF brush to extract color from</param>
        public static void SetBackgroundColor(this WebView2 webView, Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                var mediaColor = solidColorBrush.Color;
                webView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(
                    mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            }
            else
            {
                // Fallback to white if brush is not a SolidColorBrush
                webView.DefaultBackgroundColor = System.Drawing.Color.White;
            }
        }
    }
}

