namespace SOPS.Application.Abstractions;

/// <summary>
/// Per-request partner API version, set from the SOPS API version (e.g. /api/v1 → partner v1).
/// </summary>
public interface IThirdPartyApiVersionContext
{
    string? PartnerVersion { get; set; }
    string? SopsApiVersion { get; set; }
}
