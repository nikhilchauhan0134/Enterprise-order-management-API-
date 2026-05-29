using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace SOPS.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task Should_RetryUntilSuccess_When_ThirdPartyReturns500()
    {
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            return callCount < 5
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode == HttpStatusCode.InternalServerError)
            })
            .Build();

        var response = await pipeline.ExecuteAsync(
            async ct => await client.GetAsync("/orders", ct));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, callCount);
    }
}

internal sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(handler(request));
}
