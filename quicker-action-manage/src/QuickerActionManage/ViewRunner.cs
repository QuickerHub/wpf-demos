using System;
using System.Reflection;
using System.Windows;
using QuickerActionManage.View;
using QuickerActionManage.Utils;

namespace QuickerActionManage
{
    /// <summary>
    /// Runner for Quicker integration
    /// </summary>
    public static class ViewRunner
    {
        static ViewRunner()
        {
            Loader.LoadThemeXamls(Assembly.GetExecutingAssembly(), "Theme.xaml");
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
            ShowWindow(win, new WindowOptions { CanUseQuicker = true, LastSize = true });
        }

        /// <summary>
        /// Show window with options
        /// </summary>
        private static void ShowWindow(Window win, WindowOptions options)
        {
            win.SourceInitialized += (s, e) =>
            {
                if (options.CanUseQuicker)
                {
                    var handle = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        try
                        {
                            // Try to set CanUseQuicker using QWindowHelper if available
                            // This requires QWindowHelper from Quicker utilities
                            var qWindowHelperType = Type.GetType("Quicker.Utilities.QWindowHelper, Quicker.Utilities");
                            if (qWindowHelperType != null)
                            {
                                var setCanUseQuickerMethod = qWindowHelperType.GetMethod("SetCanUseQuicker", 
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                setCanUseQuickerMethod?.Invoke(null, new object[] { handle, true });
                            }
                        }
                        catch
                        {
                            // Ignore if QWindowHelper is not available
                        }
                    }
                }
            };

            if (options.LastSize)
            {
                // Load last window size from state if needed
                // This can be implemented using GlobalStateWriter or similar
            }

            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            win.Activate();
        }

        /// <summary>
        /// Window display options
        /// </summary>
        private class WindowOptions
        {
            public bool CanUseQuicker { get; set; }
            public bool LastSize { get; set; }
        }
    }
}

