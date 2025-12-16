using System.Windows;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : Window
    {
        private readonly EditorViewModel _viewModel;

        public EditorViewModel ViewModel => _viewModel;

        public EditorWindow()
        {
            InitializeComponent();
            
            // Set WebView2 default background color
            EditorWebViewControl.SetBackgroundColor(this.Background);
            
            _viewModel = new EditorViewModel();
            DataContext = this;
            
            // Start initializing WebView when window is loaded
            this.Loaded += EditorWindow_Loaded;
        }

        /// <summary>
        /// Initialize with content
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync(string text, string language = "plaintext")
        {
            await _viewModel.InitializeAsync(EditorWebViewControl, text, language);
        }

        /// <summary>
        /// Update editor content
        /// </summary>
        public async System.Threading.Tasks.Task UpdateContentAsync(string text, string? language = null)
        {
            await _viewModel.SetContentAsync(text, language);
        }

        private async void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialization will be done via InitializeAsync method
        }
    }
}

