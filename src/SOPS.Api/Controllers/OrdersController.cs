using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

/// <summary>
/// Orders API — save to DB, write cache, queue, and optional third-party REST dispatch.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController(IOrderWorkflowService workflow) : ControllerBase
{
    /// <summary>
    /// Create order: SQL → cache → channel queue → optional HttpClient POST to third-party REST API.
    /// </summary>
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

    /// <summary>Get order (cache-first, then database). Response includes data source.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await workflow.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedOrdersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await workflow.ListAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>
    /// Dispatch an existing order to the third-party REST API (HttpClient POST).
    /// </summary>
    [HttpPost("{id:guid}/dispatch")]
    [ProducesResponseType(typeof(ThirdPartyDispatchInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DispatchToThirdParty(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await workflow.DispatchToThirdPartyAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
