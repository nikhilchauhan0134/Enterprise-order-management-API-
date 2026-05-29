using SOPS.Application.DTOs;

namespace SOPS.Application.Abstractions;

public interface IBulkOrderRepository
{
    Task<int> BulkInsertAsync(IReadOnlyList<BulkOrderItem> orders, CancellationToken ct = default);
}
