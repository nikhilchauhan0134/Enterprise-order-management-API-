namespace SOPS.Application.Options;

/// <summary>
/// Third-party integration configuration. Set <see cref="ActiveVersion"/> to "v1" or "v2".
/// </summary>
public sealed class ThirdPartyOptions
{
    public const string SectionName = "ThirdParty";

    /// <summary>Which partner API version is used by default ("v1" or "v2").</summary>
    public string ActiveVersion { get; set; } = "v1";

    public ThirdPartyVersionOptions V1 { get; set; } = new()
    {
        SubmitOrderPath = "/api/v1/orders"
    };

    public ThirdPartyVersionOptions V2 { get; set; } = new()
    {
        SubmitOrderPath = "/api/v2/orders"
    };

    /// <summary>Legacy single URL — used when V1.BaseUrl is empty.</summary>
    public string BaseUrl { get; set; } = "https://httpbin.org/";

    public int RequestsPerSecond { get; set; } = 800;
    public int MaxConcurrentRequests { get; set; } = 50;
    public int HealthCacheSeconds { get; set; } = 30;
    public int MaxConnectionsPerServer { get; set; } = 50;
    public int RetryMaxAttempts { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 10;
    public int CircuitBreakerSeconds { get; set; } = 60;

    public ThirdPartyVersionOptions ResolveVersion(string? version)
    {
        var v = NormalizeVersion(version ?? ActiveVersion);
        return v == "v2" ? V2 : V1;
    }

    /// <summary>
    /// Maps SOPS API version → partner client version. Add "3.0": "v3" when you add V3 client.
    /// </summary>
    public Dictionary<string, string> SopsToPartnerVersionMap { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1.0"] = "v1",
        ["2.0"] = "v2"
    };

    public static string NormalizeVersion(string version) =>
        version.Equals("v2", StringComparison.OrdinalIgnoreCase) ? "v2" : "v1";

    /// <summary>
    /// Resolves partner version from SOPS API version (1.0 → v1, 2.0 → v2), else <see cref="ActiveVersion"/>.
    /// </summary>
    public string MapFromSopsApiVersion(string? sopsApiVersion)
    {
        if (!string.IsNullOrWhiteSpace(sopsApiVersion)
            && SopsToPartnerVersionMap.TryGetValue(sopsApiVersion.Trim(), out var partner))
            return NormalizeVersion(partner);

        return NormalizeVersion(ActiveVersion);
    }
}
