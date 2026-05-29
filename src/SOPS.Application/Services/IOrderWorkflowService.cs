using SOPS.Application.DTOs;

namespace SOPS.Application.Services;

/// <summary>
/// Orchestrates DB + cache + queue + third-party REST for use by API controllers.
/// </summary>
public interface IOrderWorkflowService
{
    Task<OrderSubmitResponse> SubmitAsync(CreateOrderRequest request, bool dispatchToThirdParty, CancellationToken ct = default);
    Task<OrderDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedOrdersResponse> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ThirdPartyDispatchInfo> DispatchToThirdPartyAsync(Guid orderId, CancellationToken ct = default);
}
