using System;
using System.Collections.Generic;
using System.Windows;
using BatchRenameTool.Template.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BatchRenameTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // Register extension class types (empty for now, will add StringExtensions in stage 3)
                    services.AddSingleton<IEnumerable<Type>>(sp => new Type[]
                    {
                        // Will add StringExtensions, NumberExtensions, etc. in later stages
                    });

                    // Register template parser (singleton)
                    services.AddSingleton<TemplateParser>(sp =>
                    {
                        var extensionTypes = sp.GetRequiredService<IEnumerable<Type>>();
                        return new TemplateParser(extensionTypes);
                    });

                    // Register ViewModel (transient)
                    services.AddTransient<ViewModels.BatchRenameViewModel>();
                })
                .Build();

            _host.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Get service from DI container (optional, for convenience)
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return ((App)Current)._host!.Services.GetRequiredService<T>();
        }
    }
}
