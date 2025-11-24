using System.Windows;

namespace QuickerExpressionAgent.Desktop.Extensions;

/// <summary>
/// Extension methods for Window
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    /// Show and activate the window, restoring it if minimized
    /// </summary>
    /// <param name="window">Window to show</param>
    public static void ShowAndActivate(this Window window)
    {
        if (window == null)
            return;

        window.Show();
        window.Activate();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }
    }
}

