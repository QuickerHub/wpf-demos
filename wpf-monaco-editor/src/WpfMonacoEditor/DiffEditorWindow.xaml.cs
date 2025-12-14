using System.Windows;

namespace WpfMonacoEditor
{
    /// <summary>
    /// Interaction logic for DiffEditorWindow.xaml
    /// </summary>
    public partial class DiffEditorWindow : Window
    {
        private readonly DiffEditorViewModel _viewModel;

        public DiffEditorViewModel ViewModel => _viewModel;

        public DiffEditorWindow()
        {
            InitializeComponent();
            
            // Set WebView2 default background color
            DiffWebViewControl.SetBackgroundColor(this.Background);
            
            _viewModel = new DiffEditorViewModel();
            DataContext = this;
            
            // Start initializing WebView when window is loaded
            this.Loaded += DiffEditorWindow_Loaded;
        }

        /// <summary>
        /// Initialize with content
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync(string originalText, string modifiedText, string language = "plaintext")
        {
            await _viewModel.InitializeAsync(DiffWebViewControl, originalText, modifiedText, language);
        }

        /// <summary>
        /// Update editor content
        /// </summary>
        public async System.Threading.Tasks.Task UpdateContentAsync(string originalText, string modifiedText, string? language = null)
        {
            await _viewModel.SetContentAsync(originalText, modifiedText, language);
        }

        private async void DiffEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialization will be done via InitializeAsync method
        }
    }
}

