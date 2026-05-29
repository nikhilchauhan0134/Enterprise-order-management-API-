using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using SOPS.Application.Abstractions;
using SOPS.Application.Options;

namespace SOPS.Infrastructure.External;

public static class ThirdPartyHttpClientExtensions
{
    public static IServiceCollection AddThirdPartyHttpClient(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddScoped<IThirdPartyApiVersionContext, ThirdPartyApiVersionContext>();

        services.Configure<ThirdPartyOptions>(config.GetSection(ThirdPartyOptions.SectionName));
        var options = config.GetSection(ThirdPartyOptions.SectionName).Get<ThirdPartyOptions>()
            ?? new ThirdPartyOptions();

        ApplyLegacyBaseUrl(options);

        RegisterVersionedClient<ThirdPartyOrderClientV1>(services, options, options.V1, "third-party-v1");
        RegisterVersionedClient<ThirdPartyOrderClientV2>(services, options, options.V2, "third-party-v2");

        // Scoped: factory depends on scoped IThirdPartyApiVersionContext (per HTTP request).
        services.AddScoped<ThirdPartyOrderClientFactory>();
        services.AddScoped<IThirdPartyOrderClientFactory>(sp => sp.GetRequiredService<ThirdPartyOrderClientFactory>());
        services.AddScoped<IThirdPartyOrderClient>(sp => sp.GetRequiredService<ThirdPartyOrderClientFactory>());

        return services;
    }

    private static void ApplyLegacyBaseUrl(ThirdPartyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.V1.BaseUrl))
            options.V1.BaseUrl = options.BaseUrl;
        if (string.IsNullOrWhiteSpace(options.V2.BaseUrl))
            options.V2.BaseUrl = options.V1.BaseUrl;
    }

    private static void RegisterVersionedClient<TClient>(
        IServiceCollection services,
        ThirdPartyOptions options,
        ThirdPartyVersionOptions versionOptions,
        string pipelineName)
        where TClient : class
    {
        var baseUrl = string.IsNullOrWhiteSpace(versionOptions.BaseUrl)
            ? "https://httpbin.org/"
            : versionOptions.BaseUrl;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            baseUrl = "https://httpbin.org/";

        services.AddHttpClient<TClient>(c =>
            {
                c.BaseAddress = new Uri(baseUrl);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = options.MaxConnectionsPerServer,
                EnableMultipleHttp2Connections = true
            })
            .AddResilienceHandler(pipelineName, pipeline => ConfigureResilience(pipeline, options));
    }

    private static void ConfigureResilience(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline,
        ThirdPartyOptions options)
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = options.RetryMaxAttempts,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode is HttpStatusCode.InternalServerError
                    or HttpStatusCode.ServiceUnavailable
                    or HttpStatusCode.TooManyRequests
                || args.Outcome.Exception is not null)
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerSeconds)
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
        pipeline.AddConcurrencyLimiter(options.MaxConcurrentRequests);
    }
}
