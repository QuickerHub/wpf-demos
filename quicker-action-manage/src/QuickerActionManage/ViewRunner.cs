using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using QuickerActionManage.View;
using QuickerActionManage.Utils;
using QuickerActionManage.State;
using Point = System.Drawing.Point;

namespace QuickerActionManage
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class ViewRunner
    {
        private static readonly GlobalStateWriter _stateWriter = new(typeof(ViewRunner).FullName);
        private static ActionManageWindow? _actionManageWindowInstance;
        private static readonly DebounceTimer _sizeSaveDebounce = new(500); // 500ms debounce for size saving

        /// <summary>
        /// Get storage key with debug suffix if not in Quicker
        /// </summary>
        private static string GetKey(string key) => QuickerUtil.CheckIsInQuicker() ? key : $"{key}_Debug";

        static ViewRunner()
        {
            Loader.LoadThemeXamls(typeof(ViewRunner).Assembly, "Theme.xaml");
        }

        /// <summary>
        /// Show action management window (singleton)
        /// </summary>
        public static void ActionManageWindow()
        {
            // Check if window already exists and is still open
            if (_actionManageWindowInstance != null)
            {
                // Check if window is still valid (not closed)
                // IsVisible is false when window is closed
                if (_actionManageWindowInstance.IsVisible)
                {
                    // Window exists and is open, activate it
                    _actionManageWindowInstance.Activate();
                    _actionManageWindowInstance.Focus();
                    // Bring window to front
                    if (_actionManageWindowInstance.WindowState == WindowState.Minimized)
                    {
                        _actionManageWindowInstance.WindowState = WindowState.Normal;
                    }
                    return;
                }
                else
                {
                    // Window was closed, clear the reference
                    _actionManageWindowInstance = null;
                }
            }

            // Create new window instance
            var win = new ActionManageWindow()
            {
                Title = "动作&公共子程序管理窗口"
            };

            // Store the instance
            _actionManageWindowInstance = win;

            // Handle window closed event to clear the instance
            win.Closed += (s, e) =>
            {
                _actionManageWindowInstance = null;
            };

            ShowWindow(win, new WindowOptions { LastSize = true });
        }

        /// <summary>
        /// Show window with options
        /// </summary>
        private static void ShowWindow(Window win, WindowOptions options)
        {
            string windowTag = win.GetType().FullName ?? "ActionManageWindow";

            if (options.LastSize)
            {
                // 恢复窗口大小
                var sizeKey = GetKey($"{windowTag}.Size");
                var sizeStr = _stateWriter.Read(sizeKey, "") as string;
                var size = String2Point(sizeStr);
                if (size != null && size.Value.X > 0 && size.Value.Y > 0)
                {
                    // Set window size directly using WPF properties
                    win.Width = size.Value.X;
                    win.Height = size.Value.Y;
                }
            }

            win.SourceInitialized += (s, e) =>
            {
                // Center window in screen after size is restored
                var handle = WindowHelper.GetHandle(win);
                if (handle != IntPtr.Zero)
                {
                    WindowHelper.CenterWindowInScreen(handle);
                }
            };

            win.Loaded += (s, e) =>
            {
                if (options.LastSize)
                {
                    // Save window size on SizeChanged event with debounce
                    win.SizeChanged += (s, e) =>
                    {
                        // Skip saving if window is minimized
                        if (win.WindowState == WindowState.Minimized)
                        {
                            return;
                        }

                        // Use debounce to avoid frequent saves during window resizing
                        _sizeSaveDebounce.DoAction(() =>
                        {
                            // Use WPF window properties directly to handle DPI scaling correctly
                            var width = (int)win.Width;
                            var height = (int)win.Height;

                            // Only save if size is valid (greater than 0)
                            if (width > 0 && height > 0)
                            {
                                var sizeKey = GetKey($"{windowTag}.Size");
                                _stateWriter.Write(sizeKey, Point2String(new Point(width, height)));
                            }
                        });
                    };
                }
            };


            win.Show();
            win.Activate();
        }

        private static string Point2String(Point? p)
        {
            return p == null ? "" : $"{p?.X},{p?.Y}";
        }

        private static Point? String2Point(string? postr)
        {
            if (string.IsNullOrEmpty(postr)) return null;
            var pos = postr.Split(',').Select(x =>
            {
                int.TryParse(x, out var val);
                return val;
            }).ToArray();
            return pos.Length >= 2 ? (Point?)new Point(pos[0], pos[1]) : null;
        }

        /// <summary>
        /// Window display options
        /// </summary>
        private class WindowOptions
        {
            public bool LastSize { get; set; }
        }
    }
}

