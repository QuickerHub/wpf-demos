using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WpfMonacoEditor
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string OriginalText { get; set; } = "";

        [ObservableProperty]
        public partial string ModifiedText { get; set; } = "";

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;

        private WebViewManager? _webViewManager;

        public async Task SetWebViewAsync(WebView2 webView)
        {
            try
            {
                IsLoading = true;
                
                // Create WebViewManager with default configuration
                _webViewManager = new WebViewManager(webView);
                
                // Subscribe to message received event
                _webViewManager.WebMessageReceived += (sender, message) =>
                {
                    // Handle messages from Monaco Editor if needed
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
                            IsLoading = false;
                            
                            // Send HandyControl theme to web page
                            if (_webViewManager != null)
                            {
                                await _webViewManager.SendThemeToWebAsync();
                            }
                        });
                    };
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UpdateEditorContent()
        {
            if (_webViewManager == null)
            {
                MessageBox.Show("WebView 未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Send content to Monaco Editor via JavaScript
                var originalJson = Newtonsoft.Json.JsonConvert.SerializeObject(OriginalText);
                var modifiedJson = Newtonsoft.Json.JsonConvert.SerializeObject(ModifiedText);
                var script = $@"
                    if (window.monacoDiffEditor) {{
                        window.monacoDiffEditor.setOriginalText({originalJson});
                        window.monacoDiffEditor.setModifiedText({modifiedJson});
                    }}
                ";
                await _webViewManager.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新编辑器内容失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void TestShowDiffEditor()
        {
            try
            {
                // Test with sample content
                var originalText = @"// Original code
function hello() {
    console.log('Hello, World!');
    return true;
}";

                var modifiedText = @"// Modified code
function hello() {
    console.log('Hello, World!');
    console.log('Modified!');
    return true;
}";

                // Show diff editor with test content
                Runner.ShowDiffEditor(originalText, modifiedText, "javascript", "test-editor-1");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

