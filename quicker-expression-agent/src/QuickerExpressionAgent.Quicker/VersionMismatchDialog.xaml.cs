using System.Windows;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Dialog for handling version mismatch between running and target versions
/// </summary>
public partial class VersionMismatchDialog : Window
{
    public enum VersionChoice
    {
        StartNewVersion,
        ContinueOldVersion
    }

    public VersionChoice? Result { get; private set; }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(VersionMismatchDialog), new PropertyMetadata(string.Empty));

    public VersionMismatchDialog(string message)
    {
        InitializeComponent();
        DataContext = this; // Set DataContext to enable binding
        Message = message;
    }

    private void StartNewVersionButton_Click(object sender, RoutedEventArgs e)
    {
        Result = VersionChoice.StartNewVersion;
        DialogResult = true;
        Close();
    }

    private void ContinueOldVersionButton_Click(object sender, RoutedEventArgs e)
    {
        Result = VersionChoice.ContinueOldVersion;
        DialogResult = true;
        Close();
    }
}

