namespace SOPS.Application.DTOs;

public sealed record ThirdPartyHealthDto(
    bool IsHealthy,
    bool SystemOnline,
    string BaseUrl,
    string PartnerApiVersion);
