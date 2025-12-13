using System.Windows;
using System.Windows.Input;

namespace WpfWebview
{
    /// <summary>
    /// Simple input dialog for URL input
    /// </summary>
    public partial class InputDialog : Window
    {
        public string? InputText { get; private set; }

        public InputDialog(string? initialText = null)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(initialText))
            {
                UrlTextBox.Text = initialText;
            }
            Loaded += (s, e) => UrlTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = UrlTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}

