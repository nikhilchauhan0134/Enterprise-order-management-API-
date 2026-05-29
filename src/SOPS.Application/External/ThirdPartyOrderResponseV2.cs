namespace SOPS.Application.External;

/// <summary>Partner API v2 response body.</summary>
public sealed record ThirdPartyOrderResponseV2(
    string Status,
    string? ExternalReference = null,
    string? ApiVersion = "v2");
