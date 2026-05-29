using Microsoft.Extensions.Configuration;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;

namespace SOPS.Application.Services;

public sealed class SystemStatusService(
    ConnectivityState connectivity,
    IOutboxRepository outbox,
    IConfiguration config) : ISystemStatusService
{
    public async Task<SystemStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var pending = await outbox.GetPendingCountAsync(ct);
        var baseUrl = config["ThirdParty:BaseUrl"] ?? "not configured";
        return new SystemStatusDto(connectivity.IsOnline, pending, baseUrl);
    }
}
