using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SOPS.Application.Abstractions;
using SOPS.Application.Options;
using SOPS.Application.Services;
using SOPS.Domain.Messages;
using SOPS.Infrastructure.Background;
using SOPS.Infrastructure.Caching;
using SOPS.Infrastructure.External;
using SOPS.Infrastructure.Persistence;

namespace SOPS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSopsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ConnectivityState>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton(Channel.CreateBounded<OrderMessage>(new BoundedChannelOptions(50_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        }));

        var sqlConnection = config.GetConnectionString("SqlServer");
        services.AddDbContextPool<AppDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(sqlConnection))
                options.UseSqlServer(sqlConnection, sql => sql.EnableRetryOnFailure(3));
            else
                options.UseInMemoryDatabase("SOPS_Dev");
        });

        var redisConnection = config.GetConnectionString("Redis");
        if (IsValidRedisConnection(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddMemoryCache();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IBulkOrderRepository, BulkOrderRepository>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderCacheService, OrderCacheService>();

        services.AddScoped<OrderService>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
        services.AddScoped<IThirdPartyIntegrationService, ThirdPartyIntegrationService>();
        services.AddScoped<ISystemStatusService, SystemStatusService>();
        services.AddScoped<BulkIngestionService>();
        services.AddScoped<IBulkOrderExportService, BulkOrderExportService>();

        services.AddThirdPartyHttpClient(config);

        services.AddHostedService<ConnectivityMonitorService>();
        services.AddHostedService<OutboxDrainService>();
        services.AddHostedService<ThirdPartyDispatchService>();

        return services;
    }

    private static bool IsValidRedisConnection(string? connectionString) =>
        !string.IsNullOrWhiteSpace(connectionString);
}
