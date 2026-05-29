using System.Threading.Channels;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.Mapping;
using SOPS.Domain.Entities;
using SOPS.Domain.Enums;
using SOPS.Domain.Messages;

namespace SOPS.Application.Services;

public sealed class OrderWorkflowService(
    Channel<OrderMessage> channel,
    IOrderRepository repo,
    IOrderCacheService cache,
    IThirdPartyOrderClientFactory thirdPartyClientFactory,
    ConnectivityState connectivity,
    TimeProvider time) : IOrderWorkflowService
{
    public async Task<OrderSubmitResponse> SubmitAsync(
        CreateOrderRequest request,
        bool dispatchToThirdParty,
        CancellationToken ct = default)
    {
        var order = Order.Create(request.CustomerId, request.Total, time.GetUtcNow().DateTime);
        await repo.AddAsync(order, ct);

        var dto = order.ToDto();
        await cache.SetOrderAsync(dto, ct);
        await cache.InvalidateListAsync(ct);

        var message = OrderMessage.From(order.Id, order.CustomerId, order.TotalAmount, order.CreatedAt);
        ThirdPartyDispatchInfo? tpResult = null;

        if (!connectivity.IsOnline)
        {
            await repo.EnqueuePersistentAsync(message, ct);
            return new OrderSubmitResponse(order.Id, OrderResultStatus.Queued, dto, true, null);
        }

        if (!channel.Writer.TryWrite(message))
            await repo.EnqueuePersistentAsync(message, ct);

        if (dispatchToThirdParty)
            tpResult = await TryDispatchThirdPartyAsync(message, ct);

        var status = tpResult?.Succeeded == false && tpResult.Attempted
            ? OrderResultStatus.Queued
            : OrderResultStatus.Accepted;

        return new OrderSubmitResponse(order.Id, status, dto, true, tpResult);
    }

    public async Task<OrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cachedOnly = await cache.TryGetOrderAsync(id, ct);
        if (cachedOnly is not null)
            return new OrderDetailResponse(cachedOnly, "cache");

        var order = await cache.GetOrderAsync(id, ct);
        if (order is null) return null;

        return new OrderDetailResponse(order, "database");
    }

    public async Task<PagedOrdersResponse> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var cached = await cache.GetOrderListAsync(page, pageSize, ct);
        if (cached is not null) return cached;

        var (items, total) = await repo.ListAsync(page, pageSize, ct);
        var response = new PagedOrdersResponse(items.Select(o => o.ToDto()).ToList(), page, pageSize, total);
        await cache.SetOrderListAsync(response, page, pageSize, ct);
        return response;
    }

    public async Task<ThirdPartyDispatchInfo> DispatchToThirdPartyAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await repo.GetByIdAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} was not found.");

        var message = OrderMessage.From(order.Id, order.CustomerId, order.TotalAmount, order.CreatedAt);
        return await TryDispatchThirdPartyAsync(message, ct);
    }

    private async Task<ThirdPartyDispatchInfo> TryDispatchThirdPartyAsync(OrderMessage message, CancellationToken ct)
    {
        if (!connectivity.IsOnline)
        {
            return new ThirdPartyDispatchInfo(
                Attempted: false,
                Succeeded: false,
                Status: null,
                ExternalReference: null,
                Error: "Third-party API is offline; order remains in local queue.");
        }

        try
        {
            var response = await thirdPartyClientFactory.GetClient().SubmitOrderAsync(message, ct);
            return new ThirdPartyDispatchInfo(
                Attempted: true,
                Succeeded: true,
                Status: response.Status,
                ExternalReference: response.ExternalReference,
                Error: null);
        }
        catch (Exception ex)
        {
            await repo.EnqueuePersistentAsync(message, ct);
            return new ThirdPartyDispatchInfo(
                Attempted: true,
                Succeeded: false,
                Status: null,
                ExternalReference: null,
                Error: ex.Message);
        }
    }
}
