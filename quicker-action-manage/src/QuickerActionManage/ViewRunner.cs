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

        static ViewRunner()
        {
            Loader.LoadThemeXamls(typeof(ViewRunner).Assembly, "Theme.xaml");
        }

        /// <summary>
        /// Show action management window
        /// </summary>
        public static void ActionManageWindow()
        {
            var win = new ActionManageWindow()
            {
                Title = "动作&公共子程序管理窗口"
            };
            ShowWindow(win, new WindowOptions { LastSize = true });
        }

        /// <summary>
        /// Show window with options
        /// </summary>
        private static void ShowWindow(Window win, WindowOptions options)
        {
            string windowTag = win.GetType().FullName ?? "ActionManageWindow";

            win.SourceInitialized += (s, e) =>
            {
                var handle = WindowHelper.GetHandle(win);
                if (handle == IntPtr.Zero) return;

                if (options.LastSize)
                {
                    // 恢复窗口大小
                    var sizeStr = _stateWriter.Read($"{windowTag}.Size", "") as string;
                    var size = String2Point(sizeStr);
                    if (size != null && size.Value.X > 0 && size.Value.Y > 0)
                    {
                        WindowHelper.SetWindowSize(handle, size.Value.X, size.Value.Y);
                    }
                }
            };

            win.Loaded += (s, e) =>
            {
                var handle = WindowHelper.GetHandle(win);
                if (handle != IntPtr.Zero)
                {
                    WindowHelper.CenterWindowInScreen(handle);
                }
            };

            win.Closing += (s, e) =>
            {
                if (options.LastSize)
                {
                    var handle = WindowHelper.GetHandle(win);
                    if (handle != IntPtr.Zero)
                    {
                        var rect = WinProperty.Get(handle).Rect;
                        _stateWriter.Write($"{windowTag}.Size", Point2String(new Point((int)rect.Width, (int)rect.Height)));
                    }
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

