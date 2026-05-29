using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Domain.Entities;

namespace SOPS.Infrastructure.Persistence;

public sealed class BulkOrderRepository(IConfiguration config, AppDbContext db) : IBulkOrderRepository
{
    public async Task<int> BulkInsertAsync(IReadOnlyList<BulkOrderItem> orders, CancellationToken ct = default)
    {
        if (orders.Count == 0) return 0;

        var connStr = config.GetConnectionString("SqlServer");
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            try
            {
                return await BulkInsertViaStoredProcedureAsync(connStr, orders, ct);
            }
            catch (SqlException)
            {
                // Fall back to EF when SP/TVP not deployed yet
            }
        }

        return await BulkInsertViaEfAsync(orders, ct);
    }

    private static async Task<int> BulkInsertViaStoredProcedureAsync(
        string connStr,
        IReadOnlyList<BulkOrderItem> orders,
        CancellationToken ct)
    {
        var table = BuildOrderTable(orders);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand("dbo.usp_BulkInsertOrders", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300
        };

        var param = cmd.Parameters.AddWithValue("@Orders", table);
        param.SqlDbType = SqlDbType.Structured;
        param.TypeName = "dbo.OrderTableType";

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int count ? count : orders.Count;
    }

    private async Task<int> BulkInsertViaEfAsync(IReadOnlyList<BulkOrderItem> orders, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var entities = orders.Select(o => Order.Create(o.CustomerId, o.TotalAmount, now, o.ExternalId)).ToList();
        db.Orders.AddRange(entities);
        await db.SaveChangesAsync(ct);
        return entities.Count;
    }

    private static DataTable BuildOrderTable(IReadOnlyList<BulkOrderItem> orders)
    {
        var table = new DataTable();
        table.Columns.Add("ExternalId", typeof(Guid));
        table.Columns.Add("CustomerId", typeof(Guid));
        table.Columns.Add("TotalAmount", typeof(decimal));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTime));

        var now = DateTime.UtcNow;
        foreach (var o in orders)
            table.Rows.Add(o.ExternalId, o.CustomerId, o.TotalAmount, o.Status, now);

        return table;
    }

    public static async Task BulkInsertViaSqlBulkCopyAsync(
        string connStr,
        IReadOnlyList<BulkOrderItem> orders,
        CancellationToken ct = default)
    {
        var table = BuildOrderTable(orders);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        using var bulk = new SqlBulkCopy(conn)
        {
            DestinationTableName = "dbo.Orders",
            BatchSize = 5_000,
            BulkCopyTimeout = 300
        };

        bulk.ColumnMappings.Add("ExternalId", "ExternalId");
        bulk.ColumnMappings.Add("CustomerId", "CustomerId");
        bulk.ColumnMappings.Add("TotalAmount", "TotalAmount");
        bulk.ColumnMappings.Add("Status", "Status");
        bulk.ColumnMappings.Add("CreatedAt", "CreatedAt");

        await bulk.WriteToServerAsync(table, ct);
    }
}
