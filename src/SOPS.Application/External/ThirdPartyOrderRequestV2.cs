namespace SOPS.Application.External;

/// <summary>Partner API v2 POST body (extended contract).</summary>
public sealed record ThirdPartyOrderRequestV2(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency = "USD",
    DateTime? SubmittedAt = null);
