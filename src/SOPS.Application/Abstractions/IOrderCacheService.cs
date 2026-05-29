using SOPS.Application.DTOs;

namespace SOPS.Application.Abstractions;

/// <summary>
/// Two-tier cache (L1 memory + L2 Redis). Write on save; read on GET (cache-aside).
/// </summary>
public interface IOrderCacheService
{
    Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default);

    /// <summary>Read only from L1/L2 — no database fallback.</summary>
    Task<OrderDto?> TryGetOrderAsync(Guid id, CancellationToken ct = default);

    /// <summary>Store order in L1 + L2 immediately after saving to the database.</summary>
    Task SetOrderAsync(OrderDto order, CancellationToken ct = default);

    Task InvalidateOrderAsync(Guid id, CancellationToken ct = default);
    Task InvalidateListAsync(CancellationToken ct = default);

    Task<PagedOrdersResponse?> GetOrderListAsync(int page, int pageSize, CancellationToken ct = default);
    Task SetOrderListAsync(PagedOrdersResponse list, int page, int pageSize, CancellationToken ct = default);
}
