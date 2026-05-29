using System.Threading.Channels;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Domain.Messages;

namespace SOPS.Application.Services;

public sealed class BulkIngestionService(
    IBulkOrderRepository bulkRepo,
    Channel<OrderMessage> channel,
    IOutboxRepository outbox,
    IOrderCacheService cache,
    ConnectivityState connectivity,
    TimeProvider time)
{
    public async Task<int> IngestAsync(IAsyncEnumerable<BulkOrderItem> orders, CancellationToken ct)
    {
        var batch = new List<BulkOrderItem>(5_000);
        var inserted = 0;

        await foreach (var item in orders.WithCancellation(ct))
        {
            batch.Add(item with { Status = "Pending" });

            if (batch.Count < 5_000) continue;

            inserted += await FlushBatchAsync(batch, ct);
            batch.Clear();
        }

        if (batch.Count > 0)
            inserted += await FlushBatchAsync(batch, ct);

        return inserted;
    }

    private async Task<int> FlushBatchAsync(List<BulkOrderItem> batch, CancellationToken ct)
    {
        var count = await bulkRepo.BulkInsertAsync(batch, ct);

        foreach (var item in batch)
        {
            var message = OrderMessage.From(
                item.ExternalId,
                item.CustomerId,
                item.TotalAmount,
                time.GetUtcNow().DateTime);

            if (!connectivity.IsOnline || !channel.Writer.TryWrite(message))
                await outbox.EnqueueAsync(message, ct);
        }

        await cache.InvalidateListAsync(ct);
        return count;
    }
}
