using QuickerExpressionAgent.Desktop.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace QuickerExpressionAgent.Desktop.Windows;

/// <summary>
/// Interaction logic for ApiConfigTemplateWindow.xaml
/// </summary>
public partial class ApiConfigTemplateWindow : FluentWindow
{
    public ApiConfigTemplate? SelectedTemplate { get; private set; }

    public ApiConfigTemplateWindow(List<ApiConfigTemplate> templates)
    {
        InitializeComponent();
        TemplateListBox.ItemsSource = templates;
        
        // Set owner window to center properly
        if (Application.Current.MainWindow != null)
        {
            Owner = Application.Current.MainWindow;
        }
    }

    private void TemplateListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        OkButton.IsEnabled = TemplateListBox.SelectedItem != null;
    }

    private void TemplateListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Double click to confirm selection
        if (TemplateListBox.SelectedItem != null)
        {
            ConfirmSelection();
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        SelectedTemplate = TemplateListBox.SelectedItem as ApiConfigTemplate;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

