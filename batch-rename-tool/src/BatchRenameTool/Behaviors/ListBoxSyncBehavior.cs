using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Behaviors
{
    /// <summary>
    /// Attached behavior for synchronizing scrolling and selection between two ListBox controls
    /// </summary>
    [AttachedDependencyProperty<ListBox, ListBox>("SyncTarget")]
    public static partial class ListBoxSyncBehavior
    {
        private static readonly DependencyProperty IsSyncingProperty =
            DependencyProperty.RegisterAttached(
                "IsSyncing",
                typeof(bool),
                typeof(ListBoxSyncBehavior),
                new PropertyMetadata(false));

        /// <summary>
        /// Callback when SyncTarget property changes
        /// </summary>
        static partial void OnSyncTargetChanged(ListBox listBox, ListBox? oldValue, ListBox? newValue)
        {
            if (listBox == null)
                return;

            // Detach old event handlers
            if (oldValue != null)
            {
                DetachEvents(listBox, oldValue);
            }

            // Attach new event handlers
            if (newValue != null)
            {
                AttachEvents(listBox, newValue);
            }
        }

        /// <summary>
        /// Attach event handlers for synchronization
        /// </summary>
        private static void AttachEvents(ListBox source, ListBox target)
        {
            // Attach scroll synchronization from source to target
            source.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((sender, e) =>
            {
                if (GetIsSyncing(source) || e.VerticalChange == 0)
                    return;

                SetIsSyncing(target, true);
                try
                {
                    var scrollViewer = GetScrollViewer(source);
                    var targetScrollViewer = GetScrollViewer(target);

                    if (scrollViewer != null && targetScrollViewer != null)
                    {
                        targetScrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
                    }
                }
                finally
                {
                    SetIsSyncing(target, false);
                }
            }), true); // Use handledEventsToo = true to catch all scroll events

            // Attach selection synchronization from source to target
            source.SelectionChanged += (sender, e) =>
            {
                if (GetIsSyncing(source))
                    return;

                SyncSelection(source, target);
            };

            // Attach reverse synchronization from target to source
            // This ensures bidirectional synchronization even if only one ListBox has SyncTarget set
            target.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((sender, e) =>
            {
                if (GetIsSyncing(target) || e.VerticalChange == 0)
                    return;

                SetIsSyncing(source, true);
                try
                {
                    var scrollViewer = GetScrollViewer(target);
                    var sourceScrollViewer = GetScrollViewer(source);

                    if (scrollViewer != null && sourceScrollViewer != null)
                    {
                        sourceScrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
                    }
                }
                finally
                {
                    SetIsSyncing(source, false);
                }
            }), true);

            target.SelectionChanged += (sender, e) =>
            {
                if (GetIsSyncing(target))
                    return;

                SyncSelection(target, source);
            };
        }

        /// <summary>
        /// Detach event handlers
        /// </summary>
        private static void DetachEvents(ListBox source, ListBox target)
        {
            // Note: In WPF, we can't easily remove specific event handlers added via AddHandler
            // This is a limitation, but typically behaviors are attached once and not frequently changed
            // For production use, you might want to track handlers in a dictionary
        }

        /// <summary>
        /// Sync selection from source ListBox to target ListBox
        /// </summary>
        private static void SyncSelection(ListBox source, ListBox target)
        {
            if (source == null || target == null)
                return;

            SetIsSyncing(target, true);
            try
            {
                // Unselect all items in target first
                target.UnselectAll();

                // Since both ListBoxes should share the same ItemsSource, we can directly select the same items
                foreach (var selectedItem in source.SelectedItems)
                {
                    // Find the item container in target ListBox
                    var container = target.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListBoxItem;
                    if (container != null)
                    {
                        container.IsSelected = true;
                    }
                    else
                    {
                        // If container not generated yet (virtualization), scroll into view first
                        target.ScrollIntoView(selectedItem);
                        // Try again after scrolling
                        container = target.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListBoxItem;
                        container?.SetValue(ListBoxItem.IsSelectedProperty, true);
                    }
                }

                // Sync SelectedIndex for single selection (helps with keyboard navigation)
                if (source.SelectedItems.Count == 1 && source.SelectedIndex >= 0)
                {
                    target.SelectedIndex = source.SelectedIndex;
                }
            }
            finally
            {
                SetIsSyncing(target, false);
            }
        }

        /// <summary>
        /// Get ScrollViewer from ListBox by traversing the visual tree
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

        /// <summary>
        /// Get IsSyncing flag
        /// </summary>
        private static bool GetIsSyncing(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSyncingProperty);
        }

        /// <summary>
        /// Set IsSyncing flag
        /// </summary>
        private static void SetIsSyncing(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSyncingProperty, value);
        }
    }
}
