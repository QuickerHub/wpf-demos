using System.Windows;

namespace BatchRenameTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Use Runner to show main window (singleton)
            Runner.ShowMainWindow();
        }
    }
}
