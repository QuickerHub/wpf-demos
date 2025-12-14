using System.Windows;

namespace WpfMonacoEditor
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
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start initialization immediately when window is loaded
            await _viewModel.SetWebViewAsync(WebViewControl);
        }
    }
}

