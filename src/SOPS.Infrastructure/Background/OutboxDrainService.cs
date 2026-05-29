using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SOPS.Application.Abstractions;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.Background;

public sealed class OutboxDrainService(
    IServiceScopeFactory scopeFactory,
    Channel<OrderMessage> channel,
    ConnectivityState connectivity,
    ILogger<OutboxDrainService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(ct))
        {
            if (!connectivity.IsOnline) continue;

            await using var scope = scopeFactory.CreateAsyncScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var batch = await outbox.DequeueBatchAsync(500, ct);

            foreach (var msg in batch)
            {
                await channel.Writer.WriteAsync(msg, ct);
                logger.LogDebug("Drained outbox message {OrderId}", msg.OrderId);
            }
        }
    }
}
