using SOPS.Application.Abstractions;

namespace SOPS.Infrastructure.External;

public sealed class ThirdPartyApiVersionContext : IThirdPartyApiVersionContext
{
    public string? PartnerVersion { get; set; }
    public string? SopsApiVersion { get; set; }
}
