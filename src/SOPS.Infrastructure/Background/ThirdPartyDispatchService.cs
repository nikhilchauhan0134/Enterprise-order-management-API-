using System.Threading.Channels;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.Options;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.Background;

/// <summary>
/// Background consumer: reads <see cref="Channel{OrderMessage}"/> and calls the third-party REST API.
/// Applies an additional token-bucket rate limit (config: ThirdParty:RequestsPerSecond).
/// </summary>
public sealed class ThirdPartyDispatchService(
    IServiceScopeFactory scopeFactory,
    Channel<OrderMessage> channel,
    ConnectivityState connectivity,
    IOptions<ThirdPartyOptions> options,
    ILogger<ThirdPartyDispatchService> logger) : BackgroundService
{
    private SemaphoreSlim? _throttle;
    private TokenBucketRateLimiter? _tpRateLimiter;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings = options.Value;
        _throttle = new SemaphoreSlim(settings.MaxConcurrentRequests, settings.MaxConcurrentRequests);
        _tpRateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = settings.RequestsPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = settings.RequestsPerSecond,
            AutoReplenishment = true
        });

        await foreach (var message in channel.Reader.ReadAllAsync(ct))
        {
            if (!connectivity.IsOnline)
            {
                await EnqueueAsync(message, ct);
                continue;
            }

            using var lease = await _tpRateLimiter.AcquireAsync(1, ct);
            if (!lease.IsAcquired)
            {
                await EnqueueAsync(message, ct);
                continue;
            }

            await _throttle.WaitAsync(ct);
            _ = DispatchAsync(message, ct);
        }
    }

    private async Task EnqueueAsync(OrderMessage message, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await outbox.EnqueueAsync(message, ct);
    }

    private async Task DispatchAsync(OrderMessage message, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IThirdPartyOrderClient>();
            await client.SubmitOrderAsync(message, ct);
            logger.LogInformation("Dispatched order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dispatch failed for {OrderId}, enqueueing to outbox", message.OrderId);
            await EnqueueAsync(message, ct);
        }
        finally
        {
            _throttle?.Release();
        }
    }

    public override void Dispose()
    {
        _tpRateLimiter?.Dispose();
        _throttle?.Dispose();
        base.Dispose();
    }
}
