using System.Threading.Channels;
using Moq;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.External;
using SOPS.Application.Services;
using SOPS.Domain.Entities;

namespace SOPS.Tests;

public sealed class OrderWorkflowServiceTests
{
    private readonly Mock<IOrderRepository> _repo = new();
    private readonly Mock<IOrderCacheService> _cache = new();
    private readonly Mock<IThirdPartyOrderClient> _thirdParty = new();
    private readonly Mock<IThirdPartyOrderClientFactory> _factory = new();
    private readonly ConnectivityState _connectivity = new();
    private readonly Channel<Domain.Messages.OrderMessage> _channel = Channel.CreateUnbounded<Domain.Messages.OrderMessage>();

    public OrderWorkflowServiceTests()
    {
        _factory.Setup(f => f.GetClient(It.IsAny<string?>())).Returns(_thirdParty.Object);
        _factory.Setup(f => f.ActiveVersion).Returns("v1");
    }

    private OrderWorkflowService CreateSut() =>
        new(_channel, _repo.Object, _cache.Object, _factory.Object, _connectivity, new FakeTimeProvider());

    [Fact]
    public async Task Submit_Should_WriteCache_And_CallThirdParty_When_Online()
    {
        _connectivity.SetOnline(true);
        _repo.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _thirdParty.Setup(t => t.SubmitOrderAsync(It.IsAny<Domain.Messages.OrderMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyOrderResponse("Accepted", "ext-1"));

        var result = await CreateSut().SubmitAsync(new CreateOrderRequest(Guid.NewGuid(), 25m), true, default);

        Assert.True(result.Cached);
        Assert.NotNull(result.ThirdParty);
        Assert.True(result.ThirdParty!.Succeeded);
        _cache.Verify(c => c.SetOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _factory.Verify(f => f.GetClient(null), Times.Once);
        _thirdParty.Verify(t => t.SubmitOrderAsync(It.IsAny<Domain.Messages.OrderMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
