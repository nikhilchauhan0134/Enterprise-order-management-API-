using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.Options;

namespace SOPS.Infrastructure.External;

/// <summary>
/// Picks partner client automatically from <see cref="IThirdPartyApiVersionContext"/> (HTTP request)
/// or falls back to <see cref="ThirdPartyOptions.ActiveVersion"/> (background jobs).
/// </summary>
public sealed class ThirdPartyOrderClientFactory(
    ThirdPartyOrderClientV1 v1,
    ThirdPartyOrderClientV2 v2,
    IThirdPartyApiVersionContext versionContext,
    IOptions<ThirdPartyOptions> options,
    ILogger<ThirdPartyOrderClientFactory> logger) : IThirdPartyOrderClientFactory, IThirdPartyOrderClient
{
    public string ActiveVersion => ThirdPartyOptions.NormalizeVersion(options.Value.ActiveVersion);

    public IThirdPartyOrderClient GetClient(string? version = null)
    {
        var resolved = ResolveVersion(version);
        logger.LogDebug(
            "Partner client {PartnerVersion} (SOPS API {SopsApiVersion})",
            resolved,
            versionContext.SopsApiVersion ?? "n/a (background)");

        return resolved == "v2" ? v2 : v1;
    }

    public Task<Application.External.ThirdPartyOrderResponse> SubmitOrderAsync(
        Domain.Messages.OrderMessage message,
        CancellationToken ct = default) =>
        GetClient().SubmitOrderAsync(message, ct);

    public Task<bool> CheckHealthAsync(CancellationToken ct = default) =>
        GetClient().CheckHealthAsync(ct);

    private string ResolveVersion(string? explicitVersion)
    {
        if (!string.IsNullOrWhiteSpace(explicitVersion))
            return ThirdPartyOptions.NormalizeVersion(explicitVersion);

        if (!string.IsNullOrWhiteSpace(versionContext.PartnerVersion))
            return ThirdPartyOptions.NormalizeVersion(versionContext.PartnerVersion);

        return ActiveVersion;
    }
}
