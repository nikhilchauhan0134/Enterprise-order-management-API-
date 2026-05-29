namespace SOPS.Application.DTOs;

public sealed record SystemStatusDto(
    bool ThirdPartyOnline,
    int OutboxPendingCount,
    string ThirdPartyBaseUrl);
