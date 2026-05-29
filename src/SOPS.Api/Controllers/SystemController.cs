using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

/// <summary>
/// System status — connectivity, outbox queue depth.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemController(ISystemStatusService status) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct) =>
        Ok(await status.GetStatusAsync(ct));
}
