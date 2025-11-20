using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using H.Pipes;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Server that hosts the IQuickerService implementation via named pipe using H.Ipc
/// This server runs in the .Quicker project and provides services to agent.exe
/// </summary>
public class QuickerServiceServer : IHostedService
{
    private readonly QuickerServiceImplementation _serviceImplementation;
    private readonly ILogger<QuickerServiceServer> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly string _serverName;
    private PipeServer<string>? _server;
    private bool _isClientConnected;

    public event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Gets whether a client is currently connected
    /// </summary>
    public bool IsClientConnected
    {
        get => _isClientConnected;
        private set
        {
            if (_isClientConnected != value)
            {
                _isClientConnected = value;
                ConnectionStatusChanged?.Invoke(this, value);
            }
        }
    }

    public QuickerServiceServer(
        QuickerServiceImplementation serviceImplementation,
        ILogger<QuickerServiceServer> logger)
    {
        _serviceImplementation = serviceImplementation;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _serverName = Constants.ServerName;
        _isClientConnected = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _server = PipeHelper.GetServer(_serverName);
        
        _server.ClientConnected += (sender, args) =>
        {
            _logger.LogInformation("Client connected to expression service server");
            IsClientConnected = true;
        };
        
        _server.ClientDisconnected += (sender, args) =>
        {
            _logger.LogInformation("Client disconnected from expression service server");
            IsClientConnected = false;
        };
        
        _serviceImplementation.Initialize(_server);
        
        // Start server in background to avoid blocking IHostedService.StartAsync
        _ = Task.Run(async () =>
        {
            try
            {
                await _server.StartAsync();
                _logger.LogInformation("Expression service server started on pipe: {PipeName}", _serverName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start expression service server");
            }
        }, _cancellationTokenSource.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        
        if (_server != null)
        {
            await _server.DisposeAsync();
        }
        
        _logger.LogInformation("Expression service server stopped");
    }
}
