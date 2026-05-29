namespace SOPS.Application.DTOs;

public sealed record CreateOrderRequest(Guid CustomerId, decimal Total);
