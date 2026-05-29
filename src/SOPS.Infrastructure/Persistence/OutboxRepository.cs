using Microsoft.EntityFrameworkCore;
using SOPS.Application.Abstractions;
using SOPS.Domain.Entities;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.Persistence;

public sealed class OutboxRepository(AppDbContext db) : IOutboxRepository
{
    public async Task EnqueueAsync(OrderMessage message, CancellationToken ct = default)
    {
        db.OutboxOrders.Add(new OutboxOrder
        {
            OrderId = message.OrderId,
            Payload = message.ToJson(),
            Status = "Pending",
            CreatedAt = message.CreatedAt,
            RetryCount = 0
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OrderMessage>> DequeueBatchAsync(int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var pending = await db.OutboxOrders
            .Where(o => o.Status == "Pending" && (o.NextRetryAt == null || o.NextRetryAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return Array.Empty<OrderMessage>();

        foreach (var item in pending)
            item.Status = "Processing";

        await db.SaveChangesAsync(ct);
        return pending.Select(o => OrderMessage.FromJson(o.Payload)).ToList();
    }

    public async Task MarkCompletedAsync(long outboxId, CancellationToken ct = default)
    {
        await db.OutboxOrders.Where(o => o.Id == outboxId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Completed"), ct);
    }

    public async Task MarkFailedAsync(long outboxId, int retryCount, CancellationToken ct = default)
    {
        var nextRetry = DateTime.UtcNow.AddSeconds(Math.Pow(2, retryCount));
        await db.OutboxOrders.Where(o => o.Id == outboxId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, "Pending")
                .SetProperty(o => o.RetryCount, retryCount)
                .SetProperty(o => o.NextRetryAt, nextRetry), ct);
    }

    public Task<int> GetPendingCountAsync(CancellationToken ct = default) =>
        db.OutboxOrders.CountAsync(o => o.Status == "Pending" || o.Status == "Processing", ct);
}
