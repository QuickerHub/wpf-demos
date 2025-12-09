using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FreeSql;
using SyncMvp.Core.Services;
using SyncMvp.Core.Sync;
using System.IO;
using System.Net.Http;
using SyncMvp.Wpf.ViewModels;

namespace SyncMvp.Wpf;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Database
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SyncMvp",
                    "database.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                var db = new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, $"Data Source={dbPath}")
                    .UseAutoSyncStructure(true)
                    .Build();

                services.AddSingleton<IFreeSql>(db);

                // Services
                services.AddSingleton<IChangeLogService, ChangeLogService>();
                services.AddSingleton<IItemService, ItemService>();

                // Sync service (configure WebDAV URL from config or environment)
                var webDavUrl = Environment.GetEnvironmentVariable("WEBDAV_URL") 
                    ?? "http://localhost:8080/webdav";
                services.AddSingleton<HttpClient>();
                services.AddSingleton<ISyncService>(sp =>
                    new WebDavSyncService(
                        sp.GetRequiredService<IChangeLogService>(),
                        sp.GetRequiredService<IItemService>(),
                        sp.GetRequiredService<ILogger<WebDavSyncService>>(),
                        sp.GetRequiredService<HttpClient>(),
                        webDavUrl));

                // ViewModels
                services.AddTransient<MainWindowViewModel>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        await _host.StartAsync();

        // Show main window
        var mainWindow = new MainWindow
        {
            DataContext = _host.Services.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
