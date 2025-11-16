using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace WpfDragDrop
{
    /// <summary>
    /// Handler for file drop operations in administrator-privileged processes
    /// </summary>
    public sealed class FileDropHandler : IDisposable
    {
        private const uint WM_COPYGLOBALDATA = 73U;
        private const uint WM_COPYDATA = 74U;
        private const uint WM_DROPFILES = 563U;
        private const uint MSGFLT_ALLOW = 1U;
        private const uint MSGFLT_ADD = 1U;
        private const uint MAX_PATH = 260;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, ref CHANGEFILTERSTRUCT pChangeFilterStruct);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilter(uint msg, uint dwFlag);

        [DllImport("shell32.dll")]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch);

        [DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        [StructLayout(LayoutKind.Sequential)]
        private struct CHANGEFILTERSTRUCT
        {
            public uint cbSize;
            public uint ExtStatus;
        }

        private FrameworkElement? _containerElement;
        private Window? _attachedWindow;
        private HwndSource? _hwndSource;
        private HwndSourceHook? _messageHook;
        private readonly bool _releaseControl;

        /// <summary>
        /// Event raised when files are dropped
        /// </summary>
        public event EventHandler<FileDropEventArgs>? FilesDropped;

        /// <summary>
        /// Gets or sets the container element that accepts file drops
        /// </summary>
        public FrameworkElement? ContainerElement
        {
            get => _containerElement;
            set
            {
                if (_containerElement != value)
                {
                    Detach();
                    _containerElement = value;
                    if (_containerElement != null)
                    {
                        Attach();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of FileDropHandler
        /// </summary>
        /// <param name="containerElement">The WPF element that will accept file drops</param>
        public FileDropHandler(FrameworkElement containerElement) : this(containerElement, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of FileDropHandler
        /// </summary>
        /// <param name="containerElement">The WPF element that will accept file drops</param>
        /// <param name="releaseControl">Whether to release the control when disposing</param>
        public FileDropHandler(FrameworkElement containerElement, bool releaseControl)
        {
            if (containerElement == null)
            {
                throw new ArgumentNullException(nameof(containerElement), "containerElement is null.");
            }

            _containerElement = containerElement;
            _releaseControl = releaseControl;

            Attach();
        }

        private void Attach()
        {
            if (_containerElement == null)
                return;

            // Wait for element to be loaded to get window handle
            if (_containerElement.IsLoaded)
            {
                AttachToWindow();
            }
            else
            {
                _containerElement.Loaded += ContainerElement_Loaded;
            }
        }

        private void ContainerElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (_containerElement != null)
            {
                _containerElement.Loaded -= ContainerElement_Loaded;
                AttachToWindow();
            }
        }

        private void AttachToWindow()
        {
            if (_containerElement == null)
                return;

            var window = Window.GetWindow(_containerElement);
            if (window == null)
                return;

            // Ensure window handle is created
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.Handle;
            
            // If handle is not created yet, wait for SourceInitialized
            if (hwnd == IntPtr.Zero)
            {
                _attachedWindow = window;
                window.SourceInitialized += Window_SourceInitialized;
                return;
            }

            AttachToHandle(hwnd);
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window)
            {
                window.SourceInitialized -= Window_SourceInitialized;
                _attachedWindow = null;
                
                // Delay attachment to ensure window is fully initialized
                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var hwnd = new WindowInteropHelper(window).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        AttachToHandle(hwnd);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void AttachToHandle(IntPtr hwnd)
        {
            // Get HwndSource for message hook
            _hwndSource = HwndSource.FromHwnd(hwnd);
            if (_hwndSource == null)
            {
                System.Diagnostics.Debug.WriteLine("FileDropHandler: Failed to get HwndSource from handle");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"FileDropHandler: Attaching to window handle 0x{hwnd.ToInt64():X}");

            // Enable global message filter first (for administrator privilege)
            // This is required for elevated processes to receive messages from lower privilege processes
            EnableGlobalWindowMessageFilter(WM_DROPFILES);
            EnableGlobalWindowMessageFilter(WM_COPYGLOBALDATA);
            EnableGlobalWindowMessageFilter(WM_COPYDATA);

            // Enable per-window message filter (more specific, preferred method)
            EnableWindowMessageFilter(hwnd, WM_DROPFILES);
            EnableWindowMessageFilter(hwnd, WM_COPYGLOBALDATA);
            EnableWindowMessageFilter(hwnd, WM_COPYDATA);

            // Hook into window messages
            _messageHook = WndProc;
            _hwndSource.AddHook(_messageHook);

            // Accept file drops (must be called after enabling message filters)
            DragAcceptFiles(hwnd, true);
            System.Diagnostics.Debug.WriteLine("FileDropHandler: DragAcceptFiles called");
            
            System.Diagnostics.Debug.WriteLine("FileDropHandler: Message filters enabled");
        }

        private void Detach()
        {
            if (_hwndSource != null && _messageHook != null)
            {
                _hwndSource.RemoveHook(_messageHook);
                _messageHook = null;
            }

            if (_attachedWindow != null)
            {
                _attachedWindow.SourceInitialized -= Window_SourceInitialized;
                _attachedWindow = null;
            }

            if (_containerElement != null)
            {
                _containerElement.Loaded -= ContainerElement_Loaded;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)WM_DROPFILES)
            {
                System.Diagnostics.Debug.WriteLine("FileDropHandler: WM_DROPFILES message received");
                HandleDropFiles(wParam);
                handled = true;
                return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private void HandleDropFiles(IntPtr hDrop)
        {
            if (hDrop == IntPtr.Zero)
                return;

            try
            {
                // Get file count
                uint fileCount = DragQueryFile(hDrop, uint.MaxValue, new StringBuilder(0), 0);

                if (fileCount == 0)
                    return;

                // Get all file paths
                var fileNames = new List<string>();
                var sb = new StringBuilder((int)MAX_PATH);

                for (uint i = 0; i < fileCount; i++)
                {
                    uint length = DragQueryFile(hDrop, i, sb, (int)MAX_PATH);
                    if (length > 0)
                    {
                        fileNames.Add(sb.ToString());
                        sb.Clear();
                    }
                }

                // Finish drag operation
                DragFinish(hDrop);

                // Raise event
                if (fileNames.Count > 0)
                {
                    FilesDropped?.Invoke(this, new FileDropEventArgs(fileNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                System.Diagnostics.Debug.WriteLine($"Error handling file drop: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable global window message filter (for all windows in the process)
        /// This is required for elevated processes to receive messages from lower privilege processes
        /// </summary>
        private static void EnableGlobalWindowMessageFilter(uint message)
        {
            bool result = ChangeWindowMessageFilter(message, MSGFLT_ADD);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                // Some errors are acceptable (e.g., message already allowed)
                System.Diagnostics.Debug.WriteLine(
                    $"ChangeWindowMessageFilter (global) failed for message {message}, error: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ChangeWindowMessageFilter (global) succeeded for message {message}");
            }
        }

        /// <summary>
        /// Enable per-window message filter (more specific, preferred method)
        /// </summary>
        private static void EnableWindowMessageFilter(IntPtr hwnd, uint message)
        {
            CHANGEFILTERSTRUCT changeFilter = new CHANGEFILTERSTRUCT
            {
                cbSize = (uint)Marshal.SizeOf(typeof(CHANGEFILTERSTRUCT))
            };

            bool result = ChangeWindowMessageFilterEx(hwnd, message, MSGFLT_ALLOW, ref changeFilter);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                // Some errors are acceptable (e.g., message already allowed)
                System.Diagnostics.Debug.WriteLine(
                    $"ChangeWindowMessageFilterEx failed for message {message}, error: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ChangeWindowMessageFilterEx succeeded for message {message}");
            }
        }

        public void Dispose()
        {
            Detach();

            if (_releaseControl && _containerElement != null)
            {
                // Note: WPF elements don't have Dispose, but we can clear the reference
                _containerElement = null;
            }
        }
    }

    /// <summary>
    /// Event arguments for file drop events
    /// </summary>
    public class FileDropEventArgs : EventArgs
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
        /// Initializes a new instance of FileDropEventArgs
        /// </summary>
        /// <param name="filePaths">Array of file paths</param>
        public FileDropEventArgs(string[] filePaths)
        {
            FilePaths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
        }
    }
}

