using System.Windows;
using System.Windows.Controls;
using CeaViewRunner.ViewModels;
using Quicker.Public.Entities;

namespace CeaViewRunner.Views;

public partial class MessageBox3mdWindow : Window
{
    public MessageBox3mdWindow()
    {
        InitializeComponent();
    }

    public MessageBox3mdModel ViewModel { get; set; } = MessageBox3mdModel.Default;

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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
