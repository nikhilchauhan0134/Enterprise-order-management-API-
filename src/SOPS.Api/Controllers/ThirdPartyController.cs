using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SOPS.Application.DTOs;
using SOPS.Application.External;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

/// <summary>
/// Third-party REST. Same route names on v1 and v2 URLs — ASP.NET picks the action by API version.
/// Partner client (v1/v2) is resolved automatically from /api/v1 vs /api/v2.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/third-party")]
public sealed class ThirdPartyController(IThirdPartyIntegrationService integration) : ControllerBase
{
    [HttpGet("info")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ThirdPartyIntegrationInfoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfo(CancellationToken ct) =>
        Ok(await integration.GetIntegrationInfoAsync(ct));

    /// <summary>Health check — uses partner v1 when called as /api/v1/third-party/health.</summary>
    [HttpGet("health")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ThirdPartyHealthDto), StatusCodes.Status200OK)]
    public Task<IActionResult> GetHealthV1(CancellationToken ct) =>
        GetHealthCore(ct);

    /// <summary>Health check — uses partner v2 when called as /api/v2/third-party/health.</summary>
    [HttpGet("health")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ThirdPartyHealthDto), StatusCodes.Status200OK)]
    public Task<IActionResult> GetHealthV2(CancellationToken ct) =>
        GetHealthCore(ct);

    /// <summary>
    /// Submit order to partner API v1.
    /// URL: POST /api/v1/third-party/orders/{orderId}
    /// </summary>
    [HttpPost("orders/{orderId:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ThirdPartyOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SubmitOrderV1(Guid orderId, CancellationToken ct) =>
        SubmitOrderCore(orderId, ct);

    /// <summary>
    /// Submit order to partner API v2 (extended contract).
    /// URL: POST /api/v2/third-party/orders/{orderId}
    /// Add v2-only logic in this method body if needed.
    /// </summary>
    [HttpPost("orders/{orderId:guid}")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(ThirdPartyOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SubmitOrderV2(Guid orderId, CancellationToken ct) =>
        SubmitOrderCore(orderId, ct);

    private async Task<IActionResult> GetHealthCore(CancellationToken ct) =>
        Ok(await integration.GetHealthAsync(partnerVersion: null, ct));

    private async Task<IActionResult> SubmitOrderCore(Guid orderId, CancellationToken ct)
    {
        try
        {
            // partnerVersion: null → factory uses SOPS API version from middleware (v1 or v2).
            var response = await integration.SubmitOrderByIdAsync(orderId, partnerVersion: null, ct);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
