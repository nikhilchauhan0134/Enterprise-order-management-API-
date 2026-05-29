using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SOPS.Application.Abstractions;

namespace SOPS.Infrastructure.Background;

public sealed class ConnectivityMonitorService(
    IServiceScopeFactory scopeFactory,
    ConnectivityState state,
    ILogger<ConnectivityMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var client = scope.ServiceProvider.GetRequiredService<IThirdPartyOrderClient>();
                var online = await client.CheckHealthAsync(stoppingToken);
                state.SetOnline(online);
                logger.LogInformation("Connectivity: {Status}", state.IsOnline ? "ONLINE" : "DEGRADED");
            }
            catch (Exception ex)
            {
                state.SetOnline(false);
                logger.LogWarning(ex, "Connectivity check failed — routing to persistent queue");
            }
        }
    }
}
