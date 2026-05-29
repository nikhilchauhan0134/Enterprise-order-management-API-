using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class BulkOrdersController(BulkIngestionService bulkService) : ControllerBase
{
    [HttpPost("bulk")]
    [EnableRateLimiting("bulk-ingest")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkIngest(CancellationToken ct)
    {
        async IAsyncEnumerable<BulkOrderItem> ReadItems()
        {
            using var reader = new StreamReader(Request.Body);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line)) continue;
                var item = JsonSerializer.Deserialize<BulkOrderItem>(line);
                if (item is not null) yield return item;
            }
        }

        var count = await bulkService.IngestAsync(ReadItems(), ct);
        return Ok(new { inserted = count });
    }
}
