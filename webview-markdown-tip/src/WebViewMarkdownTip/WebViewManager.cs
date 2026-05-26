using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebViewMarkdownTip
{
    /// <summary>
    /// Manages WebView2 initialization, navigation, and script bridge for the markdown UI.
    /// </summary>
    public class WebViewManager
    {
        private readonly WebView2 _webView;
        private readonly WebViewConfiguration _config;
        private bool _isInitialized;

        public WebViewManager(WebView2 webView)
            : this(webView, new WebViewConfiguration())
        {
        }

        public WebViewManager(WebView2 webView, WebViewConfiguration config)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Raised when the web page requests closing the host window (JSON message type action/close).
        /// </summary>
        public event EventHandler? CloseWindowRequested;

        /// <summary>
        /// Raised when plain text messages are received from the web (legacy type message).
        /// </summary>
        public event EventHandler<string>? WebMessageReceived;

        /// <summary>
        /// Raised when the React shell has registered <c>window.receiveMarkdownPayload</c> and is ready for host injection.
        /// </summary>
        public event EventHandler? UiReady;

        /// <param name="afterCoreWebViewReady">
        /// Called after CoreWebView2 is ready and host objects are registered, before navigation starts.
        /// Subscribe to <see cref="CoreWebView2.NavigationCompleted"/> here so the first navigation is observed.
        /// </param>
        public async Task InitializeAsync(Action? afterCoreWebViewReady = null)
        {
            if (_isInitialized) return;

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: _config.UserDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                SetupMessageHandler();

                _webView.CoreWebView2.AddHostObjectToScript("wpfHost", new WpfHostObject());

                _isInitialized = true;

                afterCoreWebViewReady?.Invoke();

                await LoadWebContentAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 WebView2 失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupMessageHandler()
        {
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        }

        private async Task LoadWebContentAsync()
        {
            if (_webView.CoreWebView2 == null) return;

            try
            {
                if (_config.IsDebugMode)
                {
                    var devUrl = _config.GetDevServerUrlFromFile()
                                ?? Environment.GetEnvironmentVariable("WPF_MARKDOWN_TIP_DEV_URL")
                                ?? Environment.GetEnvironmentVariable("WPF_WEBVIEW_DEV_URL")
                                ?? _config.DevServerUrl;

                    if (await IsDevelopmentServerRunningAsync(devUrl))
                    {
                        System.Diagnostics.Debug.WriteLine($"WebViewManager: Loading from dev server: {devUrl}");
                        _webView.CoreWebView2.Navigate(devUrl);
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"WebViewManager: Dev server not running at {devUrl}, falling back to local files");
                }

                var indexPath = _config.GetLocalWebFilePath();
                if (File.Exists(indexPath))
                {
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        _config.VirtualHostName,
                        _config.WebBasePath,
                        CoreWebView2HostResourceAccessKind.Allow);

                    var virtualUrl = _config.GetVirtualUrl();
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: Loading from local files: {virtualUrl}");
                    _webView.CoreWebView2.Navigate(virtualUrl);
                }
                else
                {
                    var message = _config.IsDebugMode
                        ? $"无法找到网页文件。\n\n请确保 Web 文件夹存在于: {_config.WebBasePath}\n\n或者开发服务器正在运行 ({_config.DevServerUrl})。"
                        : $"无法找到网页文件。\n\n请确保 Web 文件夹存在于: {_config.WebBasePath}";

                    MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载网页失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> IsDevelopmentServerRunningAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Push markdown payload to the React page via <c>window.receiveMarkdownPayload</c>.
        /// </summary>
        public async Task PostMarkdownPayloadAsync(string markdown, string? title)
        {
            if (_webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 未初始化");
            }

            var payload = new
            {
                markdown = markdown ?? string.Empty,
                title,
            };
            var json = JsonConvert.SerializeObject(payload);
            var script =
                "(function(){var p=JSON.parse(" + JsonConvert.SerializeObject(json) +
                ");if(window.receiveMarkdownPayload){window.receiveMarkdownPayload(p);}})();";

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        public void Reload()
        {
            _webView.CoreWebView2?.Reload();
        }

        public string? GetCurrentUrl()
        {
            return _webView.CoreWebView2?.Source;
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(messageJson))
                {
                    return;
                }

                try
                {
                    var token = JToken.Parse(messageJson);
                    if (token is JObject obj)
                    {
                        var type = obj["type"]?.ToString();
                        if (string.Equals(type, "host", StringComparison.OrdinalIgnoreCase))
                        {
                            var hostAction = obj["action"]?.ToString();
                            if (string.Equals(hostAction, "uiReady", StringComparison.OrdinalIgnoreCase))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    UiReady?.Invoke(this, EventArgs.Empty));
                            }

                            return;
                        }

                        if (string.Equals(type, "action", StringComparison.OrdinalIgnoreCase))
                        {
                            var action = obj["action"]?.ToString();
                            if (string.Equals(action, "close", StringComparison.OrdinalIgnoreCase))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                    CloseWindowRequested?.Invoke(this, EventArgs.Empty));
                            }

                            return;
                        }

                        if (string.Equals(type, "message", StringComparison.OrdinalIgnoreCase))
                        {
                            var data = obj["data"]?.ToString() ?? messageJson;
                            Application.Current.Dispatcher.Invoke(() =>
                                WebMessageReceived?.Invoke(this, data));
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Fall through to plain text
                }

                Application.Current.Dispatcher.Invoke(() =>
                    WebMessageReceived?.Invoke(this, messageJson));
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: 处理网页消息失败: {ex.Message}");
                });
            }
        }
    }

    /// <summary>
    /// Minimal COM-visible host object for optional script calls from the web layer.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WpfHostObject
    {
        public string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
