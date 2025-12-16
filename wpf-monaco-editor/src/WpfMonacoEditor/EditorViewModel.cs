using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMonacoEditor
{
    /// <summary>
    /// ViewModel for Code Editor window - single editor view
    /// </summary>
    public partial class EditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading = true;

        private WebViewManager? _webViewManager;
        private string _text = "";
        private string _language = "plaintext";

        /// <summary>
        /// Initialize WebView and set initial content
        /// Uses routing system to navigate to /editor page
        /// </summary>
        public async Task InitializeAsync(WebView2 webView, string text, string language = "plaintext")
        {
            try
            {
                IsLoading = true;
                _text = text;
                _language = language;

                // Create WebViewManager with default configuration and initial route
                _webViewManager = new WebViewManager(webView, initialRoute: "/editor");

                // Subscribe to message received event
                _webViewManager.WebMessageReceived += (sender, message) =>
                {
                    // Handle messages if needed
                };

                // Initialize WebView
                await _webViewManager.InitializeAsync();

                // Subscribe to navigation events AFTER initialization is complete
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationCompleted += async (sender, e) =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            // Wait a bit for React Router to initialize and route to load
                            await Task.Delay(200);
                            
                            // Send HandyControl theme to web page
                            await _webViewManager.SendThemeToWebAsync();
                            
                            // Store initial content in window for frontend to pick up when Monaco Editor is ready
                            var textJson = Newtonsoft.Json.JsonConvert.SerializeObject(_text);
                            var languageJson = Newtonsoft.Json.JsonConvert.SerializeObject(_language);
                            var script = $@"
                                (function() {{
                                    window.pendingEditorContent = {{
                                        text: {textJson},
                                        language: {languageJson}
                                    }};
                                    
                                    // If Monaco Editor is already ready, set content immediately
                                    if (window.monacoEditor) {{
                                        window.monacoEditor.setValue({textJson});
                                        window.monacoEditor.setLanguage({languageJson});
                                    }}
                                }})();
                            ";
                            await _webViewManager.ExecuteScriptAsync(script);
                            
                            IsLoading = false;
                        });
                    };
                }
            }
            catch
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Set editor content
        /// Ensures we're on the /editor page before setting content
        /// </summary>
        public async Task SetContentAsync(string text, string? language = null)
        {
            if (_webViewManager == null)
            {
                return;
            }

            try
            {
                _text = text;
                if (language != null)
                {
                    _language = language;
                }

                // Ensure we're on the /editor page
                var checkRouteScript = @"
                    (function() {
                        if (window.wpfRouter) {
                            var currentPath = window.wpfRouter.getCurrentPath();
                            if (currentPath !== '/editor') {
                                window.wpfRouter.navigate('/editor');
                                return false; // Need to wait for navigation
                            }
                        } else {
                            var hash = window.location.hash;
                            if (hash !== '#/editor') {
                                window.location.hash = '#/editor';
                                return false; // Need to wait for navigation
                            }
                        }
                        return true; // Already on correct page
                    })();
                ";
                
                var isOnCorrectPage = await _webViewManager.ExecuteScriptAsync(checkRouteScript);
                var isOnPage = isOnCorrectPage?.Trim('"') == "true";
                
                // If not on correct page, wait for navigation
                if (!isOnPage)
                {
                    await Task.Delay(300); // Wait for route navigation
                }

                // Send content to Monaco Editor via JavaScript
                var textJson = Newtonsoft.Json.JsonConvert.SerializeObject(text);
                var languageJson = Newtonsoft.Json.JsonConvert.SerializeObject(_language);
                
                var script = $@"
                    (function() {{
                        if (window.monacoEditor) {{
                            window.monacoEditor.setValue({textJson});
                            window.monacoEditor.setLanguage({languageJson});
                        }} else {{
                            // Store pending content for when Monaco Editor is ready
                            window.pendingEditorContent = {{
                                text: {textJson},
                                language: {languageJson}
                            }};
                        }}
                    }})();
                ";
                await _webViewManager.ExecuteScriptAsync(script);
            }
            catch
            {
                // Ignore errors when setting content
            }
        }

        /// <summary>
        /// Get current editor content
        /// </summary>
        public async Task<string> GetContentAsync()
        {
            if (_webViewManager == null)
            {
                return _text;
            }

            try
            {
                var script = @"
                    (function() {
                        if (window.monacoEditor) {
                            return window.monacoEditor.getValue() || '';
                        }
                        return '';
                    })();
                ";
                var result = await _webViewManager.ExecuteScriptAsync(script);
                // Parse JSON result if needed
                return _text;
            }
            catch
            {
                return _text;
            }
        }
    }
}

