using System.Windows;
using System.Windows.Controls;
using QuickerExpressionAgent.Desktop.ViewModels;
using DependencyPropertyGenerator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickerExpressionAgent.Desktop.Controls;

/// <summary>
/// Interaction logic for ExpressionItemControl.xaml
/// </summary>
[DependencyProperty<object>("ViewModel")]
[INotifyPropertyChanged]
public partial class ExpressionItemControl : UserControl
{
    public ExpressionItemControl()
    {
        InitializeComponent();
    }
}

