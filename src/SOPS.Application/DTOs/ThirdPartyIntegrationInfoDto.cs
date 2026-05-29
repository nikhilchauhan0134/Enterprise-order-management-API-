namespace SOPS.Application.DTOs;

public sealed record ThirdPartyIntegrationInfoDto(
    string ActiveVersion,
    string V1BaseUrl,
    string V1SubmitPath,
    string V2BaseUrl,
    string V2SubmitPath);
