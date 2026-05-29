using SOPS.Domain.Entities;
using SOPS.Domain.Messages;

namespace SOPS.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Order> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task EnqueuePersistentAsync(OrderMessage message, CancellationToken ct = default);
}
