using SOPS.Application.DTOs;
using SOPS.Application.External;

namespace SOPS.Application.Services;

public interface IThirdPartyIntegrationService
{
    Task<ThirdPartyIntegrationInfoDto> GetIntegrationInfoAsync(CancellationToken ct = default);
    Task<ThirdPartyHealthDto> GetHealthAsync(string? partnerVersion = null, CancellationToken ct = default);
    Task<ThirdPartyOrderResponse> SubmitOrderByIdAsync(
        Guid orderId,
        string? partnerVersion = null,
        CancellationToken ct = default);
}
