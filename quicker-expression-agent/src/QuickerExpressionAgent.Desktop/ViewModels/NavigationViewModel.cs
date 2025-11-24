using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using QuickerExpressionAgent.Desktop.Pages;

namespace QuickerExpressionAgent.Desktop.ViewModels;

/// <summary>
/// ViewModel for navigation window
/// </summary>
public partial class NavigationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "QuickerAgent";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems = new()
    {
        new NavigationViewItem("表达式生成器", SymbolRegular.Code24, typeof(ExpressionGeneratorPage)),
        new NavigationViewItem("API 配置", SymbolRegular.Settings24, typeof(ApiConfigPage)),
        new NavigationViewItem("测试", SymbolRegular.Beaker24, typeof(TestPage))
    };

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = new();
}

