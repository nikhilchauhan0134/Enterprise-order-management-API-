using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPS.Application.External;
using SOPS.Application.Options;
using SOPS.Domain.Messages;

namespace SOPS.Infrastructure.External;

/// <summary>Shared HTTP + health-cache logic for partner API v1 and v2 clients.</summary>
public abstract class ThirdPartyOrderClientBase
{
    protected readonly HttpClient Http;
    protected readonly IMemoryCache MemoryCache;
    protected readonly ThirdPartyOptions Options;
    protected readonly ILogger Logger;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected ThirdPartyOrderClientBase(
        HttpClient http,
        IMemoryCache memoryCache,
        IOptions<ThirdPartyOptions> options,
        ILogger logger)
    {
        Http = http;
        MemoryCache = memoryCache;
        Options = options.Value;
        Logger = logger;
    }

    protected abstract string VersionLabel { get; }
    protected abstract string HealthCacheKey { get; }
    protected abstract string HealthPath { get; }

    public abstract Task<ThirdPartyOrderResponse> SubmitOrderAsync(OrderMessage message, CancellationToken ct);

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        if (MemoryCache.TryGetValue(HealthCacheKey, out bool cached))
            return cached;

        var healthy = await ProbeHealthAsync(ct);
        MemoryCache.Set(HealthCacheKey, healthy, TimeSpan.FromSeconds(Options.HealthCacheSeconds));
        return healthy;
    }

    protected async Task<ThirdPartyOrderResponse> PostJsonAsync<TRequest>(
        string relativePath,
        TRequest body,
        Guid orderId,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = JsonContent.Create(body)
        };

        Logger.LogDebug(
            "Partner API {Version} POST {Uri}",
            VersionLabel,
            request.RequestUri);

        using var response = await Http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            Logger.LogWarning(
                "Partner API {Version} HTTP {StatusCode}: {Body}",
                VersionLabel,
                (int)response.StatusCode,
                errorBody);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<ThirdPartyOrderResponse>(JsonOptions, ct)
            ?? new ThirdPartyOrderResponse("Accepted");

        Logger.LogInformation(
            "Partner API {Version} accepted order {OrderId}, status={Status}",
            VersionLabel,
            orderId,
            result.Status);

        return result;
    }

    private async Task<bool> ProbeHealthAsync(CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
            using var response = await Http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Partner API {Version} health check failed", VersionLabel);
            return false;
        }
    }
}
