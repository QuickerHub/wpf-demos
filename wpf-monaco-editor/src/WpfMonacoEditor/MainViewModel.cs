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
                    if (!string.IsNullOrEmpty(message))
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel: 收到消息: '{message}'");
                        // Handle messages from Monaco Editor if needed
                    }
                };
                
                // Initialize WebView
                await _webViewManager.InitializeAsync();
                
                // Subscribe to navigation events AFTER initialization is complete
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationCompleted += (sender, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsLoading = false;
                        });
                    };
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"MainViewModel: 初始化失败: {ex.Message}");
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
    }
}

