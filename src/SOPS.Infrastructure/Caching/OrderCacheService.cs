using System.Collections.Frozen;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.Mapping;
using SOPS.Application.Services;

namespace SOPS.Infrastructure.Caching;

public sealed class OrderCacheService(
    IMemoryCache l1,
    IDistributedCache l2,
    IOrderRepository repo) : IOrderCacheService
{
    private static readonly FrozenDictionary<string, TimeSpan> Ttls =
        new Dictionary<string, TimeSpan>
        {
            ["order:single"] = TimeSpan.FromMinutes(2),
            ["order:list"] = TimeSpan.FromMinutes(15)
        }.ToFrozenDictionary();

    public async Task<OrderDto?> TryGetOrderAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Order(id);

        if (l1.TryGetValue(key, out OrderDto? cached))
            return cached;

        var bytes = await l2.GetAsync(key, ct);
        return bytes is null ? null : JsonSerializer.Deserialize<OrderDto>(bytes);
    }

    public async Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Order(id);

        if (l1.TryGetValue(key, out OrderDto? cached))
            return cached;

        var bytes = await l2.GetAsync(key, ct);
        if (bytes is not null)
        {
            var dto = JsonSerializer.Deserialize<OrderDto>(bytes);
            if (dto is not null)
            {
                l1.Set(key, dto, Ttls["order:single"]);
                return dto;
            }
        }

        var order = await repo.GetByIdAsync(id, ct);
        if (order is null) return null;

        var result = order.ToDto();
        await SetOrderAsync(result, ct);
        return result;
    }

    public async Task SetOrderAsync(OrderDto order, CancellationToken ct = default)
    {
        var key = CacheKeys.Order(order.Id);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(order);

        await l2.SetAsync(key, serialized,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttls["order:single"] }, ct);
        l1.Set(key, order, Ttls["order:single"]);
    }

    public Task InvalidateOrderAsync(Guid id, CancellationToken ct = default)
    {
        var key = CacheKeys.Order(id);
        l1.Remove(key);
        return l2.RemoveAsync(key, ct);
    }

    public Task InvalidateListAsync(CancellationToken ct = default)
    {
        l1.Remove(CacheKeys.OrderList);
        return l2.RemoveAsync(CacheKeys.OrderList, ct);
    }

    public async Task<PagedOrdersResponse?> GetOrderListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var key = CacheKeys.OrderListPage(page, pageSize);

        if (l1.TryGetValue(key, out PagedOrdersResponse? cached))
            return cached;

        var bytes = await l2.GetAsync(key, ct);
        if (bytes is null) return null;

        var list = JsonSerializer.Deserialize<PagedOrdersResponse>(bytes);
        if (list is not null)
            l1.Set(key, list, Ttls["order:list"]);

        return list;
    }

    public async Task SetOrderListAsync(PagedOrdersResponse list, int page, int pageSize, CancellationToken ct = default)
    {
        var key = CacheKeys.OrderListPage(page, pageSize);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(list);

        await l2.SetAsync(key, serialized,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttls["order:list"] }, ct);
        l1.Set(key, list, Ttls["order:list"]);
    }
}
