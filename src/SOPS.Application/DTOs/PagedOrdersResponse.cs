namespace SOPS.Application.DTOs;

public sealed record PagedOrdersResponse(
    IReadOnlyList<OrderDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
