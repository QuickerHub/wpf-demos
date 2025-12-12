using System.Windows;
using DependencyPropertyGenerator;

namespace BatchRenameTool.Controls;

/// <summary>
/// Input dialog for getting text input from user
/// </summary>
[DependencyProperty<string>("InputText", DefaultValue = "")]
public partial class InputDialog : Window
{
    private readonly ICompletionService _completionService;

    public InputDialog()
    {
        InitializeComponent();
        _completionService = new TemplateCompletionService();
        Loaded += InputDialog_Loaded;
    }

    private void InputDialog_Loaded(object sender, RoutedEventArgs e)
    {
        // Set completion service for TemplateInputBox
        InputBox.CompletionService = _completionService;
        
        // Focus the text editor and select all text
        InputBox.FocusEditor();
        if (!string.IsNullOrEmpty(InputBox.Text))
        {
            InputBox.SelectAll();
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
