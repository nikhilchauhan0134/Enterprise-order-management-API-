using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;

namespace SOPS.Application.Services;

public interface IBulkOrderExportService
{
    IAsyncEnumerable<OrderExportDto> StreamAllOrdersAsync(OrderExportFilter filter, CancellationToken ct = default);
}

public sealed class BulkOrderExportService(IOrderQueryService query) : IBulkOrderExportService
{
    public IAsyncEnumerable<OrderExportDto> StreamAllOrdersAsync(OrderExportFilter filter, CancellationToken ct = default) =>
        query.StreamOrdersAsync(filter, ct);
}

public interface IOrderQueryService
{
    IAsyncEnumerable<OrderExportDto> StreamOrdersAsync(OrderExportFilter filter, CancellationToken ct = default);
}
