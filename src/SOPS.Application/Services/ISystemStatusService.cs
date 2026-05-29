using SOPS.Application.DTOs;

namespace SOPS.Application.Services;

public interface ISystemStatusService
{
    Task<SystemStatusDto> GetStatusAsync(CancellationToken ct = default);
}
