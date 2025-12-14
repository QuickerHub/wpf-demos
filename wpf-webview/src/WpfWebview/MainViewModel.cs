using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WpfWebview
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial string MessageFromWeb { get; set; } = "等待网页消息...";

        [ObservableProperty]
        public partial string MessageToWeb { get; set; } = "";

        [ObservableProperty]
        public partial string CurrentUrl { get; set; } = "";

        [ObservableProperty]
        public partial bool IsLoading { get; set; } = true;

        private WebViewManager? _webViewManager;

        public async Task SetWebViewAsync(WebView2 webView)
        {
            try
            {
                IsLoading = true;
                CurrentUrl = "正在初始化...";
                
                // Create WebViewManager with default configuration
                // Configuration will automatically detect output directory and look for web files there
                _webViewManager = new WebViewManager(webView);
                
                // Subscribe to message received event
                _webViewManager.WebMessageReceived += (sender, message) =>
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        System.Diagnostics.Debug.WriteLine($"MainViewModel: 收到消息: '{message}'");
                        MessageFromWeb = message;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MainViewModel: 收到空消息，忽略");
                    }
                };
                
                // Initialize WebView (message handler is set up inside InitializeAsync)
                await _webViewManager.InitializeAsync();
                
                // Subscribe to navigation events AFTER initialization is complete
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting += (sender, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsLoading = true;
                            CurrentUrl = e.Uri ?? "加载中...";
                        });
                    };
                    
                    webView.CoreWebView2.NavigationCompleted += (sender, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsLoading = false;
                            CurrentUrl = _webViewManager?.GetCurrentUrl() ?? "加载完成";
                        });
                    };
                }
                
                // Update current URL
                CurrentUrl = _webViewManager.GetCurrentUrl() ?? "就绪";
            }
            catch (Exception ex)
            {
                IsLoading = false;
                CurrentUrl = "初始化失败";
                System.Diagnostics.Debug.WriteLine($"MainViewModel: 初始化失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SendMessageToWeb()
        {
            if (_webViewManager == null)
            {
                MessageBox.Show("WebView 未初始化", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageToWeb))
            {
                MessageBox.Show("请输入要发送的消息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await _webViewManager.SendMessageToWebAsync(MessageToWeb);
                MessageToWeb = ""; // Clear input after sending
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送消息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task NavigateToUrl()
        {
            if (_webViewManager == null) return;

            string? url = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new InputDialog("https://www.example.com");
                if (dialog.ShowDialog() == true)
                {
                    url = dialog.InputText;
                }
            });

            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    _webViewManager.Navigate(url);
                    CurrentUrl = _webViewManager.GetCurrentUrl() ?? url;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导航失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ReloadPage()
        {
            _webViewManager?.Reload();
            CurrentUrl = _webViewManager?.GetCurrentUrl() ?? "重新加载中...";
        }

        [RelayCommand]
        private void GoBack()
        {
            _webViewManager?.GoBack();
            CurrentUrl = _webViewManager?.GetCurrentUrl() ?? CurrentUrl;
        }

        [RelayCommand]
        private void GoForward()
        {
            _webViewManager?.GoForward();
            CurrentUrl = _webViewManager?.GetCurrentUrl() ?? CurrentUrl;
        }
    }
}

