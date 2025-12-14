using System.Windows;
using System.Windows.Media;

namespace WpfWebview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Set WebView2 default background color from HandyControl theme
            WebViewControl.SetBackgroundColor(this.Background);
            
            _viewModel = new MainViewModel();
            DataContext = this; // Set DataContext to this, not ViewModel
            
            // Start initializing WebView immediately after window is loaded
            // This reduces the white screen time
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start initialization immediately when window is loaded
            // Don't wait for WebView's Loaded event
            await _viewModel.SetWebViewAsync(WebViewControl);
        }

        private void NavigationUrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _viewModel.NavigateToUrl();
                e.Handled = true;
            }
        }
    }
}

