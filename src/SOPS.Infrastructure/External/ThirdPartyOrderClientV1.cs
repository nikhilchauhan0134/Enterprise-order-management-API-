using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.External;
using SOPS.Application.Options;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.External;

/// <summary>Partner REST API version 1 — POST /api/v1/orders.</summary>
public sealed class ThirdPartyOrderClientV1(
    HttpClient http,
    IMemoryCache memoryCache,
    IOptions<ThirdPartyOptions> options,
    ILogger<ThirdPartyOrderClientV1> logger)
    : ThirdPartyOrderClientBase(http, memoryCache, options, logger), IThirdPartyOrderClient
{
    protected override string VersionLabel => "v1";
    protected override string HealthCacheKey => "third-party:v1:health";
    protected override string HealthPath =>
        string.IsNullOrWhiteSpace(Options.V1.HealthPath)
            ? ThirdPartyApiRoutes.Health
            : Options.V1.HealthPath;

    public override Task<ThirdPartyOrderResponse> SubmitOrderAsync(
        OrderMessage message,
        CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(Options.V1.SubmitOrderPath)
            ? ThirdPartyApiRoutes.SubmitOrder
            : Options.V1.SubmitOrderPath;

        var body = new ThirdPartyOrderRequest(
            message.OrderId,
            message.CustomerId,
            message.Total);

        return PostJsonAsync(path, body, message.OrderId, ct);
    }
}
