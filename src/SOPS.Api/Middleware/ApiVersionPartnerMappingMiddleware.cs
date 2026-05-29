using Asp.Versioning;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.Options;

namespace SOPS.Api.Middleware;

/// <summary>
/// Maps SOPS URL API version (api/v1, api/v2) to partner integration version (v1, v2) automatically.
/// </summary>
public sealed class ApiVersionPartnerMappingMiddleware(
    RequestDelegate next,
    IOptions<ThirdPartyOptions> options,
    ILogger<ApiVersionPartnerMappingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IThirdPartyApiVersionContext versionContext)
    {
        var apiVersion = context.GetRequestedApiVersion()?.ToString();
        if (!string.IsNullOrWhiteSpace(apiVersion))
        {
            versionContext.SopsApiVersion = apiVersion;
            versionContext.PartnerVersion = options.Value.MapFromSopsApiVersion(apiVersion);
            logger.LogDebug(
                "SOPS API {SopsVersion} → partner {PartnerVersion}",
                apiVersion,
                versionContext.PartnerVersion);
        }

        await next(context);
    }
}
