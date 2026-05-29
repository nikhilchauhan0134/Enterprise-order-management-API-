using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SOPS.Api.Middleware;
using SOPS.Api.Swagger;
using SOPS.Infrastructure;
using SOPS.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.CaptureStartupErrors(true);

    builder.Host.UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(2, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetSlidingWindowLimiter("global", _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10_000,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            }));

        options.AddPolicy("per-customer", ctx =>
        {
            var customerId = ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
            return RateLimitPartition.GetTokenBucketLimiter(customerId, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 500,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 100,
                AutoReplenishment = true
            });
        });

        options.AddConcurrencyLimiter("bulk-ingest", opt =>
        {
            opt.PermitLimit = 5;
            opt.QueueLimit = 10;
        });
    });

    builder.Services.AddOutputCache();

    var sqlConnection = builder.Configuration.GetConnectionString("SqlServer");
    var healthChecks = builder.Services.AddHealthChecks();
    if (!string.IsNullOrWhiteSpace(sqlConnection))
        healthChecks.AddDbContextCheck<AppDbContext>("sql", tags: ["ready"]);

    builder.Services.AddSopsInfrastructure(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var descriptions = app.Services
                .GetRequiredService<IApiVersionDescriptionProvider>()
                .ApiVersionDescriptions;

            foreach (var description in descriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"SOPS {description.GroupName.ToUpperInvariant()}");
            }

            // Match your default API version in AddApiVersioning.
            options.RoutePrefix = "swagger";
        });
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseMiddleware<ApiVersionPartnerMappingMiddleware>();
    app.UseRateLimiter();
    app.UseOutputCache();

    app.MapControllers();

    app.MapHealthChecks("/api/v1/health");
    app.MapHealthChecks("/api/v1/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    if (!string.IsNullOrWhiteSpace(sqlConnection))
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            Log.Information("Database schema ensured");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database initialization skipped — verify ConnectionStrings:SqlServer and firewall rules");
        }
    }

    Log.Information("SOPS API starting");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
