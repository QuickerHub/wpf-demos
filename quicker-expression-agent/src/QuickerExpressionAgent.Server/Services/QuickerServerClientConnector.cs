using System;
using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services;

/// <summary>
/// Connector for the .Server project to connect to the Quicker service via named pipe
/// This is used by the .Server project to call methods implemented in the .Quicker project
/// </summary>
public class QuickerServerClientConnector
{
    private readonly PipeClient<string> _client;
    private readonly QuickerServiceClient _serviceClient;
    private readonly ILogger<QuickerServerClientConnector> _logger;

    public QuickerServerClientConnector(ILogger<QuickerServerClientConnector> logger)
    {
        _logger = logger;
        _client = PipeHelper.GetClient<string>(Constants.ServerName);
        _serviceClient = new QuickerServiceClient();
        _serviceClient.Initialize(_client);

        _client.Disconnected += OnDisconnected;
        _client.Connected += OnConnected;
    }

    /// <summary>
    /// Get the Quicker service client
    /// Automatically starts connection if not connected
    /// </summary>
    public QuickerServiceClient ServiceClient
    {
        get
        {
            StartAsync().Wait();
            return _serviceClient;
        }
    }

    /// <summary>
    /// Start the connection to the expression agent service
    /// </summary>
    public async Task StartAsync()
    {
        if (_client.IsConnected)
        {
            return;
        }

        // Try to connect with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await _client.ConnectAsync(cts.Token);
            _logger.LogInformation("Connected to expression agent service");
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Failed to connect to expression agent service");
        }
    }

    private void OnConnected(object? sender, H.Pipes.Args.ConnectionEventArgs<string> e)
    {
        _logger.LogInformation("Pipe connected");
    }

    private void OnDisconnected(object? sender, H.Pipes.Args.ConnectionEventArgs<string> e)
    {
        _logger.LogInformation("Pipe disconnected");
    }
}

