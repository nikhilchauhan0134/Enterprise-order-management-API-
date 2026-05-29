namespace SOPS.Application.DTOs;

public sealed record BulkOrderItem(
    Guid ExternalId,
    Guid CustomerId,
    decimal TotalAmount,
    string Status = "Pending");
