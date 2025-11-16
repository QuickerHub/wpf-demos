using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfDragDrop
{
    /// <summary>
    /// WPF control that supports file drop operations in administrator-privileged processes
    /// </summary>
    public class FileDropControl : ContentControl
    {
        private FileDropHandler? _fileDropHandler;

        /// <summary>
        /// Event raised when files are dropped
        /// </summary>
        public event EventHandler<FileDropRoutedEventArgs>? FilesDropped
        {
            add => AddHandler(FilesDroppedEvent, value);
            remove => RemoveHandler(FilesDroppedEvent, value);
        }

        /// <summary>
        /// FilesDropped routed event
        /// </summary>
        public static readonly RoutedEvent FilesDroppedEvent = EventManager.RegisterRoutedEvent(
            nameof(FilesDropped),
            RoutingStrategy.Bubble,
            typeof(EventHandler<FileDropRoutedEventArgs>),
            typeof(FileDropControl));

        /// <summary>
        /// Whether file drop is enabled
        /// </summary>
        public bool IsFileDropEnabled
        {
            get => (bool)GetValue(IsFileDropEnabledProperty);
            set => SetValue(IsFileDropEnabledProperty, value);
        }

        /// <summary>
        /// IsFileDropEnabled dependency property
        /// </summary>
        public static readonly DependencyProperty IsFileDropEnabledProperty =
            DependencyProperty.Register(
                nameof(IsFileDropEnabled),
                typeof(bool),
                typeof(FileDropControl),
                new PropertyMetadata(true, OnIsFileDropEnabledChanged));

        private static void OnIsFileDropEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FileDropControl control)
            {
                control.UpdateFileDropHandler();
            }
        }

        static FileDropControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FileDropControl), new FrameworkPropertyMetadata(typeof(FileDropControl)));
        }

        public FileDropControl()
        {
            Loaded += FileDropControl_Loaded;
            Unloaded += FileDropControl_Unloaded;
        }

        private void FileDropControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateFileDropHandler();
        }

        private void FileDropControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeFileDropHandler();
        }

        private void UpdateFileDropHandler()
        {
            DisposeFileDropHandler();

            if (IsFileDropEnabled && IsLoaded)
            {
                _fileDropHandler = new FileDropHandler(this);
                _fileDropHandler.FilesDropped += FileDropHandler_FilesDropped;
            }
        }

        private void FileDropHandler_FilesDropped(object? sender, FileDropEventArgs e)
        {
            // Raise routed event
            RaiseEvent(new FileDropRoutedEventArgs(FilesDroppedEvent, this, e.FilePaths));
        }

        private void DisposeFileDropHandler()
        {
            if (_fileDropHandler != null)
            {
                _fileDropHandler.FilesDropped -= FileDropHandler_FilesDropped;
                _fileDropHandler.Dispose();
                _fileDropHandler = null;
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            UpdateFileDropHandler();
        }
    }

    /// <summary>
    /// Routed event arguments for file drop events
    /// </summary>
    public class FileDropRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the array of file paths that were dropped
        /// </summary>
        public string[] FilePaths { get; }

        /// <summary>
        /// Gets the number of files dropped
        /// </summary>
        public int FileCount => FilePaths?.Length ?? 0;

        /// <summary>
        /// Initializes a new instance of FileDropRoutedEventArgs
        /// </summary>
        /// <param name="routedEvent">The routed event</param>
        /// <param name="source">The source element</param>
        /// <param name="filePaths">Array of file paths</param>
        public FileDropRoutedEventArgs(RoutedEvent routedEvent, object source, string[] filePaths)
            : base(routedEvent, source)
        {
            FilePaths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
        }
    }
}

