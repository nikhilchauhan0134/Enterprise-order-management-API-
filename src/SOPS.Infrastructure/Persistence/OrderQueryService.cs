using Microsoft.EntityFrameworkCore;
using SOPS.Application.DTOs;
using SOPS.Application.Services;

namespace SOPS.Infrastructure.Persistence;

public sealed class OrderQueryService(AppDbContext db) : IOrderQueryService
{
    public async IAsyncEnumerable<OrderExportDto> StreamOrdersAsync(
        OrderExportFilter filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        const int pageSize = 5_000;
        var lastId = Guid.Empty;
        var from = filter.From ?? DateTime.MinValue;
        var to = filter.To ?? DateTime.MaxValue;

        while (true)
        {
            var page = await db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Id > lastId)
                .OrderBy(o => o.Id)
                .Take(pageSize)
                .Select(o => new OrderExportDto(o.Id, o.Status, o.TotalAmount, o.CreatedAt))
                .ToListAsync(ct);

            if (page.Count == 0) yield break;

            foreach (var item in page)
                yield return item;

            lastId = page[^1].Id;
        }
    }
}
