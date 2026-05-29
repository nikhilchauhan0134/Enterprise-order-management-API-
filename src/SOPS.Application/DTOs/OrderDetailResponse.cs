namespace SOPS.Application.DTOs;

public sealed record OrderDetailResponse(OrderDto Order, string DataSource);
