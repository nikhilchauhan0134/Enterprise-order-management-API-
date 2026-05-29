namespace SOPS.Application.DTOs;

public sealed record OrderDto(
    Guid Id,
    Guid ExternalId,
    Guid CustomerId,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
