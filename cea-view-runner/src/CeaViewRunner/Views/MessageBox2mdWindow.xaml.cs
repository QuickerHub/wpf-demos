using System.Windows;
using System.Windows.Controls;
using CeaViewRunner.ViewModels;
using Quicker.Public.Entities;

namespace CeaViewRunner.Views;

public partial class MessageBox2mdWindow : Window
{
    public MessageBox2mdWindow()
    {
        InitializeComponent();
    }

    public MessageBox2mdModel ViewModel { get; set; } = MessageBox2mdModel.Default;

    protected override void OnContentRendered(System.EventArgs e)
    {
        base.OnContentRendered(e);
        TheButtonList.AddHandler(Button.ClickEvent, new RoutedEventHandler(Button_Click));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button btn && btn.DataContext is CommonOperationItem item)
        {
            ViewModel.Result = item.Data ?? "";
        }

        Close();
    }
}
