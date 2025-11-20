using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Service for launching the expression agent application
/// </summary>
public class ApplicationLauncher
{
    private readonly ILogger<ApplicationLauncher> _logger;
    private readonly ConfigService _configService;

    public ApplicationLauncher(
        ILogger<ApplicationLauncher> logger,
        ConfigService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    /// <summary>
    /// Start the expression agent application if not already running
    /// </summary>
    public async Task StartApplicationAsync()
    {
        // Check if application is already running using mutex
        bool createdNew;
        using (var mutex = new Mutex(true, Constants.AppMutexName, out createdNew))
        {
            if (!createdNew)
            {
                _logger.LogInformation("Application is already running");
                return;
            }
        }

        var startupPath = _configService.StartupConfig.GetStartupPath();
        if (string.IsNullOrEmpty(startupPath))
        {
            _logger.LogError("Startup executable path not found");
            return;
        }

        if (!File.Exists(startupPath))
        {
            _logger.LogError("Startup executable does not exist: {Path}", startupPath);
            return;
        }

        try
        {
            _logger.LogInformation("Starting application: {Path}", startupPath);
            Process.Start(new ProcessStartInfo(startupPath)
            {
                UseShellExecute = true
            });

            // Wait a bit for the application to start
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start application: {Path}", startupPath);
        }
    }
}

