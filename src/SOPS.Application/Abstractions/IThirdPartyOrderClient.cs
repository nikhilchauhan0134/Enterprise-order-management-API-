using SOPS.Application.External;
using SOPS.Domain.Messages;

namespace SOPS.Application.Abstractions;

/// <summary>
/// Typed HttpClient wrapper for the external REST order API (POST order, GET health).
/// </summary>
public interface IThirdPartyOrderClient
{
    Task<ThirdPartyOrderResponse> SubmitOrderAsync(OrderMessage message, CancellationToken ct = default);
    Task<bool> CheckHealthAsync(CancellationToken ct = default);
}
