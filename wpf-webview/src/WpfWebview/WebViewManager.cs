using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace WpfWebview
{
    /// <summary>
    /// Manages WebView2 initialization and web content loading
    /// </summary>
    public class WebViewManager
    {
        private readonly WebView2 _webView;
        private readonly string _webBasePath;
        private readonly string _webDevUrl;
        private readonly bool _isDebugMode;
        private bool _isInitialized;

        public WebViewManager(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            
            // Check if running in debug mode
            #if DEBUG
            _isDebugMode = true;
            #else
            _isDebugMode = false;
            #endif
            
            // Get dev server URL from environment variable, fallback to default
            _webDevUrl = Environment.GetEnvironmentVariable("WPF_WEBVIEW_DEV_URL") 
                        ?? "http://localhost:5173";
            
            // Get web folder path relative to current assembly (files are in output directory directly)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyPath = Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory;
            _webBasePath = assemblyPath;
        }

        /// <summary>
        /// Initialize WebView2 and load web content
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize WebView2 environment
                var env = await CoreWebView2Environment.CreateAsync();
                await _webView.EnsureCoreWebView2Async(env);

                // Setup message handler BEFORE loading content
                // Remove existing handler first to avoid duplicate subscriptions
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                // Add host object for JavaScript to call C# methods
                _webView.CoreWebView2.AddHostObjectToScript("wpfHost", new WpfHostObject());

                // Inject JavaScript bridge - this will execute when document is created
                // This must run BEFORE the page's JavaScript loads
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function() {
                        // Bridge for sending messages to WPF
                        window.wpfBridge = {
                            sendMessage: function(message) {
                                try {
                                    if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                                        // Ensure message is a string
                                        var messageStr = (message === null || message === undefined) ? '' : String(message);
                                        
                                        var messageObj = {
                                            type: 'message',
                                            data: messageStr
                                        };
                                        
                                        var jsonStr = JSON.stringify(messageObj);
                                        console.log('Sending message to WPF:', jsonStr);
                                        console.log('Message data value:', messageStr);
                                        console.log('Message data type:', typeof messageStr);
                                        console.log('Message data length:', messageStr.length);
                                        
                                        window.chrome.webview.postMessage(jsonStr);
                                        console.log('Message sent to WPF successfully');
                                        return true;
                                    } else {
                                        console.error('WebView postMessage not available. chrome.webview:', window.chrome?.webview);
                                        return false;
                                    }
                                } catch (e) {
                                    console.error('Error sending message to WPF:', e);
                                    console.error('Error stack:', e.stack);
                                    return false;
                                }
                            }
                        };

                        // Method for C# to call to send messages to JavaScript
                        window.receiveFromWpf = function(messageObj) {
                            try {
                                if (window.onWpfMessage) {
                                    window.onWpfMessage(messageObj);
                                } else {
                                    console.warn('window.onWpfMessage is not defined');
                                }
                            } catch (e) {
                                console.error('Error receiving message from WPF:', e);
                            }
                        };

                        console.log('WPF Bridge initialized successfully');
                        
                        // Dispatch custom event to notify that bridge is ready
                        if (typeof window.dispatchEvent !== 'undefined') {
                            window.dispatchEvent(new CustomEvent('wpfBridgeReady'));
                        }
                    })();
                ");

                // Also inject after navigation completes to ensure bridge is available
                _webView.CoreWebView2.NavigationCompleted += async (sender, e) =>
                {
                    if (e.IsSuccess)
                    {
                        // Verify bridge is available, if not, inject again
                        try
                        {
                            var bridgeCheck = await _webView.CoreWebView2.ExecuteScriptAsync(@"
                                (function() {
                                    if (!window.wpfBridge) {
                                        window.wpfBridge = {
                                            sendMessage: function(message) {
                                                try {
                                                    if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                                                        // Ensure message is a string
                                                        var messageStr = (message === null || message === undefined) ? '' : String(message);
                                                        window.chrome.webview.postMessage(JSON.stringify({
                                                            type: 'message',
                                                            data: messageStr
                                                        }));
                                                        return true;
                                                    }
                                                    return false;
                                                } catch (e) {
                                                    console.error('Error sending message:', e);
                                                    return false;
                                                }
                                            }
                                        };
                                        console.log('WPF Bridge re-initialized after navigation');
                                    }
                                    return window.wpfBridge ? 'ready' : 'not ready';
                                })();
                            ");
                            System.Diagnostics.Debug.WriteLine($"Bridge check result: {bridgeCheck}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error checking bridge: {ex.Message}");
                        }
                    }
                };

                _isInitialized = true;

                // Load web content
                await LoadWebContentAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 WebView2 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                if (_isDebugMode)
                {
                    // Get dev server URL from Vite's output file or environment variable
                    var devUrl = GetDevServerUrlFromFile() 
                                ?? Environment.GetEnvironmentVariable("WPF_WEBVIEW_DEV_URL")
                                ?? _webDevUrl;
                    
                    // Check if dev server is running (no delay, check immediately)
                    if (await IsDevelopmentServerRunningAsync(devUrl))
                    {
                        System.Diagnostics.Debug.WriteLine($"WebViewManager: Loading from dev server: {devUrl}");
                        _webView.CoreWebView2.Navigate(devUrl);
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"WebViewManager: Dev server not running at {devUrl}, falling back to local files");
                    }
                }

                // Release mode or dev server not available: Load from local Web folder
                var indexPath = Path.Combine(_webBasePath, "index.html");
                if (File.Exists(indexPath))
                {
                    // Map virtual host name to local folder for proper resource loading
                    // This allows relative paths in HTML to work correctly
                    const string virtualHostName = "app.webview.local";
                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        virtualHostName,
                        _webBasePath,
                        CoreWebView2HostResourceAccessKind.Allow);

                    // Navigate using the virtual host name
                    var virtualUrl = $"http://{virtualHostName}/index.html";
                    System.Diagnostics.Debug.WriteLine($"WebViewManager: Loading from local files: {virtualUrl}");
                    _webView.CoreWebView2.Navigate(virtualUrl);
                }
                else
                {
                    var message = _isDebugMode
                        ? $"无法找到网页文件。\n\n请确保 Web 文件夹存在于: {_webBasePath}\n\n或者开发服务器正在运行 (http://localhost:5173)。"
                        : $"无法找到网页文件。\n\n请确保 Web 文件夹存在于: {_webBasePath}";
                    
                    MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载网页失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get dev server URL from Vite's output file (.vite-dev-server)
        /// </summary>
        private string? GetDevServerUrlFromFile()
        {
            try
            {
                // Try multiple possible locations for .vite-dev-server file
                var possiblePaths = new[]
                {
                    // Relative to web project (when running from source)
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "WpfWebview.Web", ".vite-dev-server"),
                    // Alternative relative path
                    Path.Combine(
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                        "..", "..", "WpfWebview.Web", ".vite-dev-server"),
                    // Direct path from assembly location
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "WpfWebview.Web", ".vite-dev-server")
                };
                
                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        var url = File.ReadAllText(fullPath).Trim();
                        if (!string.IsNullOrEmpty(url))
                        {
                            System.Diagnostics.Debug.WriteLine($"WebViewManager: Found dev server URL in file: {url} (from {fullPath})");
                            return url;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebViewManager: Error reading dev server file: {ex.Message}");
            }
            
            return null;
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

        /// <summary>
        /// Setup message handler (called after initialization)
        /// </summary>
        public void SetupMessageHandler()
        {
            // Message handler is already set up in InitializeAsync
            // This method is kept for backward compatibility
        }

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
                            
                            System.Diagnostics.Debug.WriteLine($"WebViewManager: 提取的消息内容: '{message}' (长度: {message?.Length ?? 0})");
                            
                            // Capture message in local variable to avoid closure issues
                            var finalMessage = message;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                System.Diagnostics.Debug.WriteLine($"WebViewManager: 触发 WebMessageReceived 事件，消息: '{finalMessage}'");
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
                    MessageBox.Show($"处理网页消息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            
            if (_webView?.CoreWebView2 != null)
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
                MessageBox.Show(message, "来自 JavaScript 的消息", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            return $"WPF 已收到消息: {message}";
        }

        public string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}

