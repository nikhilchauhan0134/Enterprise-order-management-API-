using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersExportController(IBulkOrderExportService exportService) : ControllerBase
{
    [HttpGet("export")]
    [Produces("application/x-ndjson")]
    public async Task Export(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var filter = new OrderExportFilter(from, to);
        Response.ContentType = "application/x-ndjson";

        await foreach (var dto in exportService.StreamAllOrdersAsync(filter, ct))
        {
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(dto) + "\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
