using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuickerExpressionAgent.Desktop.Extensions;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure services
            var services = new ServiceCollection();
            services.AddDesktopServices();
            
            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            
            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Create and show main window
            var mainWindow = new MainWindow(_serviceProvider);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
