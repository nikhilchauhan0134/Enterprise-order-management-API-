using Microsoft.EntityFrameworkCore;
using SOPS.Application.Abstractions;
using SOPS.Domain.Entities;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.Persistence;

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<(IReadOnlyList<Order> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task EnqueuePersistentAsync(OrderMessage message, CancellationToken ct = default)
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
}
