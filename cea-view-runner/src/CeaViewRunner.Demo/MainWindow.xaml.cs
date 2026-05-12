using System.Windows;
using CeaViewRunner;
using CeaViewRunner.Infrastructure;

namespace CeaViewRunner.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnMessageBox3OkCancel(object sender, RoutedEventArgs e)
    {
        var r = ViewRunner.MessageBox3mdOkCancel(this, "## 确认\n\n是否继续？", true);
        MessageBox.Show($"isOk={r.isOk}, doNotRemind={r.doNotRemind}");
    }

    private void OnMessageBox3Show(object sender, RoutedEventArgs e)
    {
        ViewRunner.MessageBox3md(
            new CustomWindowParam("show", autoCloseTime: 4, showLoc: "CenterScreen")
            {
                StartUpLocation = WindowLocations.CenterScreen,
            },
            markdown: "### 提示\n\n几秒后自动关闭。",
            window: new { maxHeight = 320, padding = "20" },
            customButtons: "关闭|Cancel");
    }

    private void OnTimeWindow(object sender, RoutedEventArgs e)
    {
        ViewRunner.TimeWindow(
            new CustomWindowParam("show")
            {
                StartUpLocation = WindowLocations.CenterScreen,
                Topmost = true,
            },
            type: "now",
            duration: "00:00:02",
            tips: "当前时间",
            useLauner: true);
    }

    private void OnGuides(object sender, RoutedEventArgs e) => ViewRunner.ShowGuidesWindow();
}
