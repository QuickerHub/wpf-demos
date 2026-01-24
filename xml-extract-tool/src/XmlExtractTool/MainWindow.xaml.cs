using System.IO;
using System.Windows;
using ICSharpCode.AvalonEdit;
using XmlExtractTool.ViewModels;

namespace XmlExtractTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly XmlExtractViewModel _viewModel;

        public XmlExtractViewModel ViewModel => _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new XmlExtractViewModel();
            DataContext = this;

            // Initialize AvalonEdit Document
            if (XmlTextEditor.Document == null)
            {
                XmlTextEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
            }

            // Bind AvalonEdit TextEditor to ViewModel
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.XmlText))
                {
                    if (XmlTextEditor.Document.Text != _viewModel.XmlText)
                    {
                        XmlTextEditor.Document.Text = _viewModel.XmlText ?? string.Empty;
                    }
                }
            };

            // Sync TextEditor changes to ViewModel (with debounce to avoid infinite loop)
            bool isUpdating = false;
            XmlTextEditor.TextChanged += (s, e) =>
            {
                if (!isUpdating && _viewModel.XmlText != XmlTextEditor.Document.Text)
                {
                    isUpdating = true;
                    _viewModel.XmlText = XmlTextEditor.Document.Text;
                    isUpdating = false;
                }
            };
        }

        /// <summary>
        /// Load input (file path or XML text) and display results
        /// </summary>
        public void LoadInput(string input)
        {
            _viewModel.LoadInput(input);
        }
    }
}
