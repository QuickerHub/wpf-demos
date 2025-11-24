using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using ListBox = System.Windows.Controls.ListBox;

namespace QuickerExpressionAgent.Desktop.Extensions;

/// <summary>
/// Extension methods for ListBox
/// </summary>
public static class ListBoxExtensions
{
    /// <summary>
    /// Scroll ListBox to bottom safely, handling ItemContainerGenerator state
    /// </summary>
    public static void ScrollToBottom(this ListBox listBox)
    {
        if (listBox == null)
            return;

        try
        {
            // Wait for ItemContainerGenerator to be ready
            if (listBox.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                // If not ready, schedule another attempt
                listBox.Dispatcher.InvokeOnLoaded(() => listBox.ScrollToBottom());
                return;
            }

            if (listBox.Items.Count > 0)
            {
                // Try to get the ScrollViewer and use ScrollToEnd (more reliable than ScrollIntoView)
                var scrollViewer = GetScrollViewer(listBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
                }
                else
                {
                    // Fallback to ScrollIntoView
                    var lastItem = listBox.Items[listBox.Items.Count - 1];
                    listBox.ScrollIntoView(lastItem);
                }
            }
        }
        catch
        {
            // Ignore scroll errors during collection updates
        }
    }

    /// <summary>
    /// Get the ScrollViewer from a ListBox by traversing the visual tree
    /// </summary>
    private static ScrollViewer? GetScrollViewer(DependencyObject depObj)
    {
        if (depObj == null)
            return null;

        // If the current object is a ScrollViewer, return it
        if (depObj is ScrollViewer scrollViewer)
            return scrollViewer;

        // Recursively search children
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            var result = GetScrollViewer(child);
            if (result != null)
                return result;
        }

        return null;
    }
}

