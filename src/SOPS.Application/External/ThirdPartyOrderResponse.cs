namespace SOPS.Application.External;

/// <summary>REST response from the third-party order API after POST /orders.</summary>
public sealed record ThirdPartyOrderResponse(
    string Status,
    string? ExternalReference = null);
