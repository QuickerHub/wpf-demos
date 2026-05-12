using System;
using System.Windows;

namespace WebViewMarkdownTip
{
    public partial class App : Application
    {
        /// <summary>
        /// Debug WinExe used to always open <see cref="MainWindow"/> with <see cref="MainViewModel"/>'s default demo markdown,
        /// which looked like "test content" and conflicted with <see cref="Runner.ShowMarkdownTip"/> windows.
        /// Open the demo only with <c>--demo</c> or env <c>WEBVIEW_MARKDOWN_TIP_DEMO=1</c> (see Properties/launchSettings.json for F5).
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!ShouldOpenDemoWindow(e.Args))
            {
                return;
            }

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private static bool ShouldOpenDemoWindow(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--demo", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return string.Equals(
                Environment.GetEnvironmentVariable("WEBVIEW_MARKDOWN_TIP_DEMO"),
                "1",
                StringComparison.Ordinal);
        }
    }
}
