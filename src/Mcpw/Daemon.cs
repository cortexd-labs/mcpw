using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mcpw;

/// <summary>
/// Windows Service host. Wraps McpServer in an IHostedService lifecycle.
/// SCM stop signal and Ctrl+C both flow through the CancellationToken.
/// </summary>
public sealed class Daemon : BackgroundService
{
    private readonly McpServer _server;
    private readonly ILogger<Daemon> _logger;

    public Daemon(McpServer server, ILogger<Daemon> logger)
    {
        _server = server;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("mcpw starting — listening on stdio");

        var reader = new StreamReader(Console.OpenStandardInput(),  leaveOpen: true);
        var writer = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true) { AutoFlush = false };

        try
        {
            await _server.RunAsync(reader, writer, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "mcpw fatal error");
            throw;
        }
        finally
        {
            _logger.LogInformation("mcpw stopped");
        }
    }
}
