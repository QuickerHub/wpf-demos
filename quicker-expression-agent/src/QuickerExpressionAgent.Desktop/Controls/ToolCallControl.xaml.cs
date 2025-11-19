using DependencyPropertyGenerator;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop.Controls;

/// <summary>
/// Interaction logic for ToolCallControl.xaml
/// </summary>
[DependencyProperty<object>("ViewModel")]
public partial class ToolCallControl : System.Windows.Controls.UserControl
{
    public ToolCallControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Typed accessor for ViewModel property
    /// </summary>
    public ToolCallViewModel? ToolCallViewModel
    {
        get => ViewModel as ToolCallViewModel;
        set => ViewModel = value;
    }
}

