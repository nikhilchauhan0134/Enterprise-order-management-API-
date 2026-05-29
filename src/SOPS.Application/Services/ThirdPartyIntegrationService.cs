using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.External;
using SOPS.Application.Options;
using SOPS.Domain.Messages;

namespace SOPS.Application.Services;

public sealed class ThirdPartyIntegrationService(
    IThirdPartyOrderClientFactory clientFactory,
    IThirdPartyApiVersionContext versionContext,
    IOrderRepository repo,
    ConnectivityState connectivity,
    IOptions<ThirdPartyOptions> options) : IThirdPartyIntegrationService
{
    public Task<ThirdPartyIntegrationInfoDto> GetIntegrationInfoAsync(CancellationToken ct = default)
    {
        var o = options.Value;
        return Task.FromResult(new ThirdPartyIntegrationInfoDto(
            clientFactory.ActiveVersion,
            o.V1.BaseUrl,
            o.V1.SubmitOrderPath,
            o.V2.BaseUrl,
            o.V2.SubmitOrderPath));
    }

    public async Task<ThirdPartyHealthDto> GetHealthAsync(
        string? partnerVersion = null,
        CancellationToken ct = default)
    {
        var partner = ResolvePartnerLabel(partnerVersion);
        var healthy = await clientFactory.GetClient(partnerVersion).CheckHealthAsync(ct);
        var versionOptions = options.Value.ResolveVersion(partner);

        return new ThirdPartyHealthDto(
            healthy,
            connectivity.IsOnline,
            versionOptions.BaseUrl,
            partner);
    }

    public async Task<ThirdPartyOrderResponse> SubmitOrderByIdAsync(
        Guid orderId,
        string? partnerVersion = null,
        CancellationToken ct = default)
    {
        var order = await repo.GetByIdAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} was not found.");

        var message = OrderMessage.From(order.Id, order.CustomerId, order.TotalAmount, order.CreatedAt);
        return await clientFactory.GetClient(partnerVersion).SubmitOrderAsync(message, ct);
    }

    private string ResolvePartnerLabel(string? explicitVersion) =>
        ThirdPartyOptions.NormalizeVersion(
            explicitVersion
            ?? versionContext.PartnerVersion
            ?? clientFactory.ActiveVersion);
}
