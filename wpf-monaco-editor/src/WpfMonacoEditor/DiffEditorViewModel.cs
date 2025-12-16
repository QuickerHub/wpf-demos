using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMonacoEditor
{
    /// <summary>
    /// ViewModel for DiffEditor window - reusable for multiple editor instances
    /// </summary>
    public partial class DiffEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading = true;

        private WebViewManager? _webViewManager;
        private string _originalText = "";
        private string _modifiedText = "";
        private string _language = "plaintext";

        /// <summary>
        /// Initialize WebView and set initial content
        /// Uses routing system to navigate to /diff page
        /// </summary>
        public async Task InitializeAsync(WebView2 webView, string originalText, string modifiedText, string language = "plaintext")
        {
            try
            {
                IsLoading = true;
                _originalText = originalText;
                _modifiedText = modifiedText;
                _language = language;

                // Create WebViewManager with default configuration and initial route
                _webViewManager = new WebViewManager(webView, initialRoute: "/diff");

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
                            var originalJson = Newtonsoft.Json.JsonConvert.SerializeObject(_originalText);
                            var modifiedJson = Newtonsoft.Json.JsonConvert.SerializeObject(_modifiedText);
                            var languageJson = Newtonsoft.Json.JsonConvert.SerializeObject(_language);
                            var script = $@"
                                (function() {{
                                    window.pendingDiffContent = {{
                                        original: {originalJson},
                                        modified: {modifiedJson},
                                        language: {languageJson}
                                    }};
                                    
                                    // If Monaco Editor is already ready, set content immediately
                                    if (window.monacoDiffEditor) {{
                                        window.monacoDiffEditor.setOriginalText({originalJson});
                                        window.monacoDiffEditor.setModifiedText({modifiedJson});
                                        window.monacoDiffEditor.setLanguage({languageJson});
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
        /// Ensures we're on the /diff page before setting content
        /// </summary>
        public async Task SetContentAsync(string originalText, string modifiedText, string? language = null)
        {
            if (_webViewManager == null)
            {
                return;
            }

            try
            {
                _originalText = originalText;
                _modifiedText = modifiedText;
                if (language != null)
                {
                    _language = language;
                }

                // Ensure we're on the /diff page
                var checkRouteScript = @"
                    (function() {
                        if (window.wpfRouter) {
                            var currentPath = window.wpfRouter.getCurrentPath();
                            if (currentPath !== '/diff') {
                                window.wpfRouter.navigate('/diff');
                                return false; // Need to wait for navigation
                            }
                        } else {
                            var hash = window.location.hash;
                            if (hash !== '#/diff') {
                                window.location.hash = '#/diff';
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
                var originalJson = Newtonsoft.Json.JsonConvert.SerializeObject(originalText);
                var modifiedJson = Newtonsoft.Json.JsonConvert.SerializeObject(modifiedText);
                var languageJson = Newtonsoft.Json.JsonConvert.SerializeObject(_language);
                
                var script = $@"
                    (function() {{
                        if (window.monacoDiffEditor) {{
                            window.monacoDiffEditor.setOriginalText({originalJson});
                            window.monacoDiffEditor.setModifiedText({modifiedJson});
                            window.monacoDiffEditor.setLanguage({languageJson});
                        }} else {{
                            // Store pending content for when Monaco Editor is ready
                            window.pendingDiffContent = {{
                                original: {originalJson},
                                modified: {modifiedJson},
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
        public async Task<(string original, string modified)> GetContentAsync()
        {
            if (_webViewManager == null)
            {
                return (_originalText, _modifiedText);
            }

            try
            {
                var script = @"
                    (function() {
                        if (window.monacoDiffEditor) {
                            return {
                                original: window.monacoDiffEditor.getOriginalText() || '',
                                modified: window.monacoDiffEditor.getModifiedText() || ''
                            };
                        }
                        return { original: '', modified: '' };
                    })();
                ";
                var result = await _webViewManager.ExecuteScriptAsync(script);
                // Parse JSON result if needed
                return (_originalText, _modifiedText);
            }
            catch
            {
                return (_originalText, _modifiedText);
            }
        }
    }
}

