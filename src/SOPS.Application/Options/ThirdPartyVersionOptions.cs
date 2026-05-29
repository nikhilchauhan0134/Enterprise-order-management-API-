namespace SOPS.Application.Options;

/// <summary>Per-version settings for a partner REST API (v1, v2, etc.).</summary>
public sealed class ThirdPartyVersionOptions
{
    public string BaseUrl { get; set; } = "https://httpbin.org/";
    public string HealthPath { get; set; } = "/health";
    public string SubmitOrderPath { get; set; } = "/api/v1/orders";
}
