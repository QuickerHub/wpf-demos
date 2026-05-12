using System.Collections.Generic;
using System.Windows;

namespace WebViewMarkdownTip
{
    /// <summary>
    /// Entry points for Quicker actions or external callers.
    /// </summary>
    public static class Runner
    {
        /// <summary>
        /// Show a modal-less markdown tip window rendered by WebView2 + React.
        /// </summary>
        /// <param name="markdown">Markdown body.</param>
        /// <param name="title">Window title (optional).</param>
        /// <param name="buttonDefinitions">
        /// Optional button lines parsed like MessageBox3md CustomButtons (e.g. <c>确认(_S)|Ok</c> per line).
        /// Parsed with Quicker <c>CommonOperationItem.ParseLines(IList&lt;string&gt;)</c>.
        /// </param>
        public static void ShowMarkdownTip(
            string markdown,
            string? title = null,
            List<string>? buttonDefinitions = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var buttons = MarkdownTipButtonDefinitions.Parse(buttonDefinitions);
                var vm = new MainViewModel(markdown, title ?? "提示", buttons);
                var window = new MainWindow(vm);
                window.Show();
            });
        }
    }
}
