namespace SOPS.Application.Abstractions;

/// <summary>
/// Resolves the correct partner API client (v1 or v2) for a request.
/// </summary>
public interface IThirdPartyOrderClientFactory
{
    string ActiveVersion { get; }

    IThirdPartyOrderClient GetClient(string? version = null);
}
