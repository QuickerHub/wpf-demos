using System;
using System.Windows;

namespace WpfDragDrop
{
    /// <summary>
    /// Attached behavior for enabling file drop on any FrameworkElement
    /// </summary>
    public static class FileDropBehavior
    {
        /// <summary>
        /// Gets whether file drop is enabled
        /// </summary>
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets whether file drop is enabled
        /// </summary>
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// IsEnabled attached property
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(FileDropBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    if (element.GetValue(FileDropHandlerProperty) is FileDropHandler handler)
                    {
                        // Already has handler, just update
                        handler.ContainerElement = element;
                    }
                    else
                    {
                        // Create new handler
                        var newHandler = new FileDropHandler(element);
                        newHandler.FilesDropped += (sender, args) =>
                        {
                            // Raise attached event
                            element.RaiseEvent(new FileDropRoutedEventArgs(
                                FilesDroppedEvent,
                                element,
                                args.FilePaths));
                        };
                        element.SetValue(FileDropHandlerProperty, newHandler);
                    }
                }
                else
                {
                    // Dispose handler
                    if (element.GetValue(FileDropHandlerProperty) is FileDropHandler handler)
                    {
                        handler.Dispose();
                        element.SetValue(FileDropHandlerProperty, null);
                    }
                }
            }
        }

        private static readonly DependencyProperty FileDropHandlerProperty =
            DependencyProperty.RegisterAttached(
                "FileDropHandler",
                typeof(FileDropHandler),
                typeof(FileDropBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// Event raised when files are dropped
        /// </summary>
        public static readonly RoutedEvent FilesDroppedEvent = EventManager.RegisterRoutedEvent(
            "FilesDropped",
            RoutingStrategy.Bubble,
            typeof(EventHandler<FileDropRoutedEventArgs>),
            typeof(FileDropBehavior));

        /// <summary>
        /// Adds FilesDropped event handler
        /// </summary>
        public static void AddFilesDroppedHandler(DependencyObject d, EventHandler<FileDropRoutedEventArgs> handler)
        {
            if (d is UIElement element)
            {
                element.AddHandler(FilesDroppedEvent, handler);
            }
        }

        /// <summary>
        /// Removes FilesDropped event handler
        /// </summary>
        public static void RemoveFilesDroppedHandler(DependencyObject d, EventHandler<FileDropRoutedEventArgs> handler)
        {
            if (d is UIElement element)
            {
                element.RemoveHandler(FilesDroppedEvent, handler);
            }
        }
    }
}

