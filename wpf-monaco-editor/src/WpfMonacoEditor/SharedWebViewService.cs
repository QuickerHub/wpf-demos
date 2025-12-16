using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Shared WebView2 Service - Singleton pattern for managing a single WebView2 instance
    /// This reduces memory usage when multiple controls need to use Monaco Editor
    /// 
    /// Usage:
    ///   var service = SharedWebViewService.Instance;
    ///   await service.InitializeAsync(webView);
    ///   await service.NavigateToAsync("/diff");
    /// </summary>
    public class SharedWebViewService
    {
        private static SharedWebViewService? _instance;
        private WebView2? _currentWebView;
        private WebViewManager? _webViewManager;
        private bool _isInitialized;

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static SharedWebViewService Instance => _instance ??= new SharedWebViewService();

        private SharedWebViewService()
        {
            // Private constructor for singleton
        }

        /// <summary>
        /// Initialize WebView2 (only once)
        /// </summary>
        public async Task InitializeAsync(WebView2 webView)
        {
            if (_isInitialized && _currentWebView == webView)
            {
                return; // Already initialized with this WebView
            }

            if (_isInitialized && _currentWebView != null && _currentWebView != webView)
            {
                // If already initialized with a different WebView, you might want to:
                // 1. Throw an exception
                // 2. Or transfer the WebViewManager to the new WebView
                // For now, we'll just update the reference
                _currentWebView = webView;
                return;
            }

            _currentWebView = webView;
            _webViewManager = new WebViewManager(webView);
            await _webViewManager.InitializeAsync();
            _isInitialized = true;
        }

        /// <summary>
        /// Navigate to a specific route using the router API
        /// </summary>
        /// <param name="route">Route path (e.g., "/diff", "/editor", "/")</param>
        public async Task NavigateToAsync(string route)
        {
            if (!_isInitialized || _webViewManager == null)
            {
                throw new InvalidOperationException("WebView2 未初始化。请先调用 InitializeAsync。");
            }

            if (string.IsNullOrWhiteSpace(route))
            {
                route = "/";
            }

            // Ensure route starts with /
            if (!route.StartsWith("/"))
            {
                route = "/" + route;
            }

            try
            {
                // Use router API to navigate
                var script = $@"
                    (function() {{
                        if (window.wpfRouter) {{
                            window.wpfRouter.navigate('{route}');
                        }} else {{
                            // Fallback: direct navigation
                            window.location.hash = '#{route}';
                        }}
                    }})();
                ";

                await _webViewManager.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"导航到路由 '{route}' 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current route path
        /// </summary>
        public async Task<string> GetCurrentRouteAsync()
        {
            if (!_isInitialized || _webViewManager == null)
            {
                throw new InvalidOperationException("WebView2 未初始化。请先调用 InitializeAsync。");
            }

            try
            {
                var script = @"
                    (function() {
                        if (window.wpfRouter) {
                            return window.wpfRouter.getCurrentPath();
                        } else {
                            return window.location.hash.replace('#', '') || '/';
                        }
                    })();
                ";

                var result = await _webViewManager.ExecuteScriptAsync(script);
                // Remove quotes from JSON string result
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取当前路由失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute JavaScript in the WebView
        /// </summary>
        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (!_isInitialized || _webViewManager == null)
            {
                throw new InvalidOperationException("WebView2 未初始化。请先调用 InitializeAsync。");
            }

            return await _webViewManager.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Send message to web page
        /// </summary>
        public async Task SendMessageAsync(string message)
        {
            if (!_isInitialized || _webViewManager == null)
            {
                throw new InvalidOperationException("WebView2 未初始化。请先调用 InitializeAsync。");
            }

            await _webViewManager.SendMessageToWebAsync(message);
        }

        /// <summary>
        /// Subscribe to web messages
        /// </summary>
        public event EventHandler<string>? WebMessageReceived
        {
            add
            {
                if (_webViewManager != null)
                {
                    _webViewManager.WebMessageReceived += value;
                }
            }
            remove
            {
                if (_webViewManager != null)
                {
                    _webViewManager.WebMessageReceived -= value;
                }
            }
        }

        /// <summary>
        /// Get the current WebView2 instance
        /// </summary>
        public WebView2? CurrentWebView => _currentWebView;

        /// <summary>
        /// Check if service is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Reset service (for testing or cleanup)
        /// </summary>
        public void Reset()
        {
            _currentWebView = null;
            _webViewManager = null;
            _isInitialized = false;
        }
    }
}

