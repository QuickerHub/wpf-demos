using System.Windows;

namespace QuickerExpressionAgent.Quicker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Start launcher and initialize services
            Launcher.Start();
            
            // Get MainWindow from DI container and show it
            var mainWindow = Launcher.GetService<MainWindow>();
            
            // Handle window closed event to exit application
            mainWindow.Closed += (sender, args) =>
            {
                Shutdown();
            };
            
            mainWindow.Show();
        }
    }
}

