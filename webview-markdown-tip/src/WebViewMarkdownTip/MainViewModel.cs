using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace WebViewMarkdownTip
{
    public partial class MainViewModel : ObservableObject
    {
        private WebViewManager? _webViewManager;
        private readonly IReadOnlyList<MarkdownTipWebButton> _buttons;

        [ObservableProperty]
        public partial string TitleText { get; set; }

        [ObservableProperty]
        public partial string MarkdownBody { get; set; }

        /// <summary>
        /// Raised when the user clicks a custom footer button; payload is <see cref="MarkdownTipWebButton.Data"/> (e.g. Ok / Cancel).
        /// Fired before <see cref="CloseRequested"/>.
        /// </summary>
        public event EventHandler<TipButtonResultEventArgs>? TipButtonResult;

        /// <summary>
        /// Raised when the web UI requests closing the window.
        /// </summary>
        public event EventHandler? CloseRequested;

        public MainViewModel()
            : this(DefaultDemoMarkdown, "Markdown 提示")
        {
        }

        public MainViewModel(string markdownBody, string titleText)
            : this(markdownBody, titleText, Array.Empty<MarkdownTipWebButton>())
        {
        }

        public MainViewModel(string markdownBody, string titleText, IReadOnlyList<MarkdownTipWebButton> buttons)
        {
            MarkdownBody = markdownBody;
            TitleText = titleText;
            _buttons = buttons;
        }

        /// <summary>
        /// Footer buttons rendered in WPF (same model as web payload previously used).
        /// </summary>
        public IReadOnlyList<MarkdownTipWebButton> FooterButtons => _buttons;

        public bool FooterHasButtons => _buttons.Count > 0;

        public async Task SetWebViewAsync(WebView2 webView)
        {
            try
            {
                _webViewManager = new WebViewManager(webView);
                _webViewManager.CloseWindowRequested += (_, _) =>
                    Application.Current.Dispatcher.Invoke(() => CloseRequested?.Invoke(this, EventArgs.Empty));
                _webViewManager.UiReady += WebView_UiReady;

                await _webViewManager.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetWebViewAsync: {ex.Message}");
                MessageBox.Show($"初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void WebView_UiReady(object? sender, EventArgs e)
        {
            if (_webViewManager == null)
            {
                return;
            }

            try
            {
                await _webViewManager.PostMarkdownPayloadAsync(MarkdownBody, TitleText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PostMarkdownPayloadAsync: {ex.Message}");
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void TipFooterButton(string? data)
        {
            TipButtonResult?.Invoke(this, new TipButtonResultEventArgs(data ?? string.Empty));
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private const string DefaultDemoMarkdown =
            "## 欢迎使用\n\n这是一个 **WebView2** + **React** 渲染的 Markdown 提示窗口。\n\n" +
            "- 支持列表\n- `代码`\n\n```ts\nconst ok = true;\n```\n";
    }

    /// <summary>
    /// Event args for <see cref="MainViewModel.TipButtonResult"/>.
    /// </summary>
    public sealed class TipButtonResultEventArgs : EventArgs
    {
        public TipButtonResultEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }
}
