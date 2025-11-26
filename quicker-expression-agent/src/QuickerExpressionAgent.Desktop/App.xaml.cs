using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickerExpressionAgent.Desktop.Extensions;
using QuickerExpressionAgent.Desktop.Services;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private NotifyIconService? _notifyIconService;
        private bool _isSilentStart;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check for silent start argument
            _isSilentStart = e.Args.Length > 0 &&
                           (e.Args[0] == "--silent" || e.Args[0] == "-s" || e.Args[0].ToLower() == "silent");

            // Build host using Host Builder pattern
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDesktopServices();
                })
                .Build();

            // Start hosted services
            await _host.StartAsync();

            // Always create MainWindow (even in silent start) so NotifyIcon can be registered
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;

            // Initialize and show tray icon (NotifyIcon is in MainWindow)
            _notifyIconService = _host.Services.GetRequiredService<NotifyIconService>();

            // Wait for MainWindow to load before showing tray icon
            mainWindow.Loaded += (s, e) =>
            {
                _notifyIconService.Show();
            };

            // Only show main window if not silent start
            if (!_isSilentStart)
            {
                mainWindow.ShowWindow();

                // Navigate to default page
                var navigationService = _host.Services.GetRequiredService<Wpf.Ui.INavigationService>();
                navigationService.Navigate(typeof(QuickerExpressionAgent.Desktop.Pages.ExpressionGeneratorPage));
            }
            else
            {
                // In silent start, still need to show window (but hidden) so NotifyIcon can be registered
                // The window will be hidden immediately after loading
                mainWindow.Show(); // Show briefly to trigger Loaded event, then hide
                mainWindow.Hide();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _notifyIconService?.Dispose();

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
