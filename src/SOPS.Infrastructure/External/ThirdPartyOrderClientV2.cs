using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.External;
using SOPS.Application.Options;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.External;

/// <summary>Partner REST API version 2 — POST /api/v2/orders (extended JSON contract).</summary>
public sealed class ThirdPartyOrderClientV2(
    HttpClient http,
    IMemoryCache memoryCache,
    IOptions<ThirdPartyOptions> options,
    ILogger<ThirdPartyOrderClientV2> logger)
    : ThirdPartyOrderClientBase(http, memoryCache, options, logger), IThirdPartyOrderClient
{
    protected override string VersionLabel => "v2";
    protected override string HealthCacheKey => "third-party:v2:health";
    protected override string HealthPath =>
        string.IsNullOrWhiteSpace(Options.V2.HealthPath)
            ? ThirdPartyApiRoutesV2.Health
            : Options.V2.HealthPath;

    public override async Task<ThirdPartyOrderResponse> SubmitOrderAsync(
        OrderMessage message,
        CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(Options.V2.SubmitOrderPath)
            ? ThirdPartyApiRoutesV2.SubmitOrder
            : Options.V2.SubmitOrderPath;

        var body = ThirdPartyOrderMapper.ToV2Request(message);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };

        Logger.LogDebug("Partner API v2 POST {Uri}", request.RequestUri);

        using var response = await Http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            Logger.LogWarning("Partner API v2 HTTP {StatusCode}: {Body}", (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var v2 = await response.Content.ReadFromJsonAsync<ThirdPartyOrderResponseV2>(JsonOptions, ct)
            ?? new ThirdPartyOrderResponseV2("Accepted");

        return ThirdPartyOrderMapper.ToV1Response(v2);
    }
}
