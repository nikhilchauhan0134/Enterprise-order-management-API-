using System.Threading.Channels;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.Mapping;
using SOPS.Domain.Entities;
using SOPS.Domain.Messages;

namespace SOPS.Application.Services;

public sealed class OrderService(
    Channel<OrderMessage> channel,
    IOrderRepository repo,
    IOrderCacheService cache,
    ConnectivityState connectivity,
    TimeProvider time)
{
    public async Task<OrderResult> SubmitAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var order = Order.Create(req.CustomerId, req.Total, time.GetUtcNow().DateTime);
        await repo.AddAsync(order, ct);

        // Cache-aside: write to cache right after DB save so GET returns without hitting SQL.
        var dto = order.ToDto();
        await cache.SetOrderAsync(dto, ct);
        await cache.InvalidateListAsync(ct);

        var message = OrderMessage.From(order.Id, order.CustomerId, order.TotalAmount, order.CreatedAt);

        if (!connectivity.IsOnline)
        {
            await repo.EnqueuePersistentAsync(message, ct);
            return OrderResult.Queued(order.Id);
        }

        if (!channel.Writer.TryWrite(message))
            await repo.EnqueuePersistentAsync(message, ct);

        return OrderResult.Accepted(order.Id);
    }

    public Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct) =>
        cache.GetOrderAsync(id, ct);

    public async Task<PagedOrdersResponse> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var cached = await cache.GetOrderListAsync(page, pageSize, ct);
        if (cached is not null)
            return cached;

        var (items, total) = await repo.ListAsync(page, pageSize, ct);
        var response = new PagedOrdersResponse(items.Select(o => o.ToDto()).ToList(), page, pageSize, total);
        await cache.SetOrderListAsync(response, page, pageSize, ct);
        return response;
    }
}

public static class CacheKeys
{
    public const string OrderList = "order:list";
    public static string Order(Guid id) => $"order:{id}";
    public static string OrderListPage(int page, int pageSize) => $"order:list:{page}:{pageSize}";
}
