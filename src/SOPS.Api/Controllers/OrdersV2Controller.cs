using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

/// <summary>
/// Orders API v2 — same route names as v1. Partner v2 is selected automatically from /api/v2/...
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersV2Controller(IOrderWorkflowService workflow) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("per-customer")]
    [ProducesResponseType(typeof(OrderSubmitResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Submit(
        [FromBody] CreateOrderRequest request,
        [FromQuery] bool dispatchToThirdParty = true,
        CancellationToken ct = default)
    {
        var result = await workflow.SubmitAsync(request, dispatchToThirdParty, ct);
        return Accepted(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        await workflow.GetByIdAsync(id, ct) is { } result ? Ok(result) : NotFound();

    [HttpGet]
    [ProducesResponseType(typeof(PagedOrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await workflow.ListAsync(page, pageSize, ct));

    [HttpPost("{id:guid}/dispatch")]
    [ProducesResponseType(typeof(ThirdPartyDispatchInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DispatchToThirdParty(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await workflow.DispatchToThirdPartyAsync(id, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
