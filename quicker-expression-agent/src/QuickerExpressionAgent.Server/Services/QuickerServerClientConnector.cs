using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using H.Pipes;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Connector for the .Server project to connect to the Quicker service via named pipe using H.Ipc
/// This is used by the .Server project to call methods implemented in the .Quicker project
/// Automatically connects and reconnects in background via IHostedService
/// </summary>
public class QuickerServerClientConnector : IHostedService
{
    private readonly PipeClient<string> _client;
    private readonly QuickerServiceClient _service;
    private readonly ILogger<QuickerServerClientConnector> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public QuickerServerClientConnector(ILogger<QuickerServerClientConnector> logger)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Create pipe client and service client
        _client = PipeHelper.GetClient(Constants.ServerName);
        _service = new QuickerServiceClient();
        _service.Initialize(_client);
        
        // Handle connection events
        _client.Connected += (sender, args) => _logger.LogInformation("Connected to expression agent service");
        _client.Disconnected += (sender, args) =>
        {
            _logger.LogWarning("Disconnected from expression agent service");
            _ = Task.Run(async () => await EnsureConnectedAsync());
        };
    }

    /// <summary>
    /// Get the Quicker service client
    /// </summary>
    public IQuickerService ServiceClient => _service;

    /// <summary>
    /// Start the hosted service and connect in background
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(EnsureConnectedAsync, cancellationToken);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stop the hosted service
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
        
        _logger.LogInformation("Quicker server client connector stopped");
    }

    /// <summary>
    /// Ensure connection is established, automatically reconnect if needed
    /// </summary>
    private async Task EnsureConnectedAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (_client.IsConnected)
            {
                await Task.Delay(1000, _cancellationTokenSource.Token);
                continue;
            }

            using var cts = new CancellationTokenSource(5000);
            try
            {
                await _client.ConnectAsync(cts.Token);
                _logger.LogInformation("Connected to expression agent service");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Connection timeout to expression agent service, will retry");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to expression agent service, will retry");
            }

            // Wait before retrying
            try
            {
                await Task.Delay(2000, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

