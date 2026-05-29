using SOPS.Domain.Messages;

namespace SOPS.Application.Abstractions;

public interface IOutboxRepository
{
    Task EnqueueAsync(OrderMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OrderMessage>> DequeueBatchAsync(int batchSize, CancellationToken ct = default);
    Task MarkCompletedAsync(long outboxId, CancellationToken ct = default);
    Task MarkFailedAsync(long outboxId, int retryCount, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
}
