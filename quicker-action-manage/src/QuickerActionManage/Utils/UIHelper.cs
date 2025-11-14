using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickerActionManage.Utils
{
    /// <summary>
    /// UI helper methods
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Find visual parent of specified type
        /// </summary>
        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        /// <summary>
        /// Check if object is on ListBoxItem
        /// </summary>
        public static bool IsOnListBoxItem(object obj)
        {
            return FindVisualParent<ListBoxItem>((DependencyObject)obj) != null;
        }
    }
}

