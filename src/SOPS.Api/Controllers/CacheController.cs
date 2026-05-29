using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.Mapping;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

/// <summary>
/// Cache management — L1 memory + L2 Redis (read / write / invalidate).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cache")]
public sealed class CacheController(IOrderCacheService cache, IOrderRepository repo) : ControllerBase
{
    /// <summary>Read order from cache only (no database).</summary>
    [HttpGet("orders/{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCachedOrder(Guid id, CancellationToken ct)
    {
        var order = await cache.TryGetOrderAsync(id, ct);
        return order is null ? NotFound(new { message = "Order not in cache" }) : Ok(order);
    }

    /// <summary>Reload order from database and store in cache.</summary>
    [HttpPost("orders/{id:guid}/refresh")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshCache(Guid id, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(id, ct);
        if (order is null) return NotFound();

        var dto = order.ToDto();
        await cache.SetOrderAsync(dto, ct);
        return Ok(dto);
    }

    /// <summary>Remove order and list entries from cache.</summary>
    [HttpDelete("orders/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InvalidateOrder(Guid id, CancellationToken ct)
    {
        await cache.InvalidateOrderAsync(id, ct);
        await cache.InvalidateListAsync(ct);
        return NoContent();
    }
}
