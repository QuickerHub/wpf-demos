using System.Threading;
using System.Threading.Tasks;
using H.Pipes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Quicker;

/// <summary>
/// Server that hosts the IQuickerService implementation via named pipe
/// This server runs in the .Quicker project and provides services to agent.exe
/// </summary>
public class QuickerServiceServer : IHostedService
{
    private readonly PipeServer<string> _server;
    private readonly QuickerServiceImplementation _serviceImplementation;
    private readonly ILogger<QuickerServiceServer> _logger;

    public QuickerServiceServer(
        QuickerServiceImplementation serviceImplementation,
        ILogger<QuickerServiceServer> logger)
    {
        _serviceImplementation = serviceImplementation;
        _logger = logger;
        _server = PipeHelper.GetServer<string>(Constants.ServerName);
        _serviceImplementation.Initialize(_server);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _server.StartAsync();
        _logger.LogInformation("Expression service server started on pipe: {PipeName}", Constants.ServerName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _server.DisposeAsync();
        _logger.LogInformation("Expression service server stopped");
    }
}

