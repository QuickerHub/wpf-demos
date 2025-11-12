using System.Windows;

namespace WindowAttach
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppState.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppState.Cleanup();
            base.OnExit(e);
        }
    }
}

