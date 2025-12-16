using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Manages WebView2 initialization and web content loading
    /// Provides unified interface for development and production environments
    /// </summary>
    public class WebViewManager
    {
        private readonly WebView2 _webView;
        private readonly WebViewConfiguration _config;
        private bool _isInitialized;
        private string? _initialRoute;

        /// <summary>
        /// Create WebViewManager with default configuration
        /// </summary>
        public WebViewManager(WebView2 webView, string? initialRoute = null)
            : this(webView, new WebViewConfiguration(), initialRoute)
        {
        }

        /// <summary>
        /// Create WebViewManager with custom configuration
        /// </summary>
        public WebViewManager(WebView2 webView, WebViewConfiguration config, string? initialRoute = null)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _initialRoute = initialRoute;
        }

        /// <summary>
        /// Initialize WebView2 and load web content
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize WebView2 environment with user data folder in assembly directory
                // This avoids permission issues in release mode
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: _config.UserDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                // Setup message handler BEFORE loading content
                SetupMessageHandler();

                // Add host object for JavaScript to call C# methods
                _webView.CoreWebView2.AddHostObjectToScript("wpfHost", new WpfHostObject());

                _isInitialized = true;

                // Load web content
                await LoadWebContentAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 WebView2 失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Setup message handler for web messages
        /// </summary>
        private void SetupMessageHandler()
        {
            // Remove existing handler first to avoid duplicate subscriptions
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        }

        /// <summary>
        /// Load web content from local folder or development server
        /// </summary>
        private async Task LoadWebContentAsync()
        {
            if (_webView.CoreWebView2 == null) return;

            try
            {
                // Debug mode: Try to load from development server first
                if (_config.IsDebugMode)
                {
                    var devUrl = _config.GetDevServerUrlFromFile()
                                ?? Environment.GetEnvironmentVariable("WPF_MONACO_DEV_URL")
                                ?? _config.DevServerUrl;

                    // Check if dev server is running
                    if (await IsDevelopmentServerRunningAsync(devUrl))
                    {
                        // Add initial route to URL if specified
                        var finalUrl = devUrl;
                        if (!string.IsNullOrEmpty(_initialRoute))
                        {
                            var route = _initialRoute.StartsWith("/") ? _initialRoute : "/" + _initialRoute;
                            finalUrl = $"{devUrl}#{route}";
                        }
                        System.Diagnostics.Debug.WriteLine($"WebViewManager: Loading from dev server: {finalUrl}");
                        _webView.CoreWebView2.Navigate(finalUrl);
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"WebViewManager: Dev server not running at {devUrl}, falling back to local files");
                    }
                }

                // Release mode or dev server not available: Load from local files
                var indexPath = _config.GetLocalWebFilePath();
                if (File.Exists(indexPath))
                {
                    // Map virtual host name to local folder for proper resource loading
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        _config.VirtualHostName,
                        _config.WebBasePath,
                        CoreWebView2HostResourceAccessKind.Allow);

                    // Navigate using the virtual host name
                    var virtualUrl = _config.GetVirtualUrl();
                    // Add initial route to URL if specified
                    if (!string.IsNullOrEmpty(_initialRoute))
                    {
                        var route = _initialRoute.StartsWith("/") ? _initialRoute : "/" + _initialRoute;
                        virtualUrl = $"{virtualUrl}#{route}";
                    }
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

        /// <summary>
        /// Check if development server is running at the specified URL
        /// </summary>
        private async Task<bool> IsDevelopmentServerRunningAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Use shorter timeout for faster fallback to local files
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    // Use HEAD request instead of GET for faster check
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
        /// Send message to JavaScript
        /// </summary>
        public async Task SendMessageToWebAsync(string message)
        {
            if (_webView?.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 未初始化");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("消息不能为空", nameof(message));
            }

            try
            {
                // Escape the message for JavaScript
                var escapedMessage = message
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r");

                // Call JavaScript function to receive message
                await _webView.CoreWebView2.ExecuteScriptAsync(
                    $"window.receiveFromWpf({{type: 'wpfMessage', data: '{escapedMessage}'}});");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"发送消息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Subscribe to web message received event
        /// </summary>
        public event EventHandler<string>? WebMessageReceived;

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(messageJson))
                {
                    System.Diagnostics.Debug.WriteLine("WebViewManager: 收到空消息");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"WebViewManager: 收到原始消息: {messageJson}");

                // Try to parse JSON message
                try
                {
                    var messageObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(messageJson);
                    if (messageObj != null)
                    {
                        var type = messageObj.type?.ToString();
                        System.Diagnostics.Debug.WriteLine($"WebViewManager: 消息类型: {type}");

                        if (type == "message")
                        {
                            var data = messageObj.data;
                            var message = data?.ToString();

                            // Ensure message is not null or empty
                            if (string.IsNullOrEmpty(message))
                            {
                                System.Diagnostics.Debug.WriteLine("WebViewManager: 消息数据为空，使用原始 JSON");
                                message = messageJson;
                            }

                            System.Diagnostics.Debug.WriteLine(
                                $"WebViewManager: 提取的消息内容: '{message}' (长度: {message?.Length ?? 0})");

                            // Capture message in local variable to avoid closure issues
                            var finalMessage = message;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"WebViewManager: 触发 WebMessageReceived 事件，消息: '{finalMessage}'");
                                WebMessageReceived?.Invoke(this, finalMessage);
                            });
                            return;
                        }
                    }
                }
                catch (Newtonsoft.Json.JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: JSON 解析失败: {jsonEx.Message}");
                    // If not JSON, use as plain text
                }

                // Use as plain text message
                System.Diagnostics.Debug.WriteLine($"WebViewManager: 使用纯文本消息: {messageJson}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WebMessageReceived?.Invoke(this, messageJson);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: 处理网页消息失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: 异常堆栈: {ex.StackTrace}");
                    MessageBox.Show($"处理网页消息失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Reload web page
        /// </summary>
        public void Reload()
        {
            _webView?.CoreWebView2?.Reload();
        }

        /// <summary>
        /// Navigate back
        /// </summary>
        public void GoBack()
        {
            if (_webView?.CoreWebView2?.CanGoBack == true)
            {
                _webView.CoreWebView2.GoBack();
            }
        }

        /// <summary>
        /// Navigate forward
        /// </summary>
        public void GoForward()
        {
            if (_webView?.CoreWebView2?.CanGoForward == true)
            {
                _webView.CoreWebView2.GoForward();
            }
        }

        /// <summary>
        /// Navigate to URL
        /// </summary>
        public void Navigate(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            if (_webView?.CoreWebView2 != null && url != null)
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
                {
                    url = "https://" + url;
                }
                _webView.CoreWebView2.Navigate(url);
            }
        }

        /// <summary>
        /// Get current URL
        /// </summary>
        public string? GetCurrentUrl()
        {
            return _webView?.CoreWebView2?.Source;
        }

        /// <summary>
        /// Execute JavaScript in the web view
        /// </summary>
        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (_webView?.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 未初始化");
            }

            try
            {
                return await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"执行脚本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Send HandyControl theme to web page
        /// </summary>
        public async Task SendThemeToWebAsync()
        {
            if (_webView?.CoreWebView2 == null)
            {
                return;
            }

            try
            {
                var theme = ThemeHelper.GetCurrentTheme();
                var themeJson = Newtonsoft.Json.JsonConvert.SerializeObject(theme);
                var script = $@"
                    (function() {{
                        if (window.setWpfTheme) {{
                            window.setWpfTheme({themeJson});
                        }} else {{
                            // Store theme for later use
                            window.wpfTheme = {themeJson};
                        }}
                    }})();
                ";
                await ExecuteScriptAsync(script);
            }
            catch
            {
                // Ignore errors when sending theme
            }
        }
    }

    /// <summary>
    /// Host object for JavaScript to call C# methods
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class WpfHostObject
    {
        public string ShowMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "来自 JavaScript 的消息", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
            return $"WPF 已收到消息: {message}";
        }

        public string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}

