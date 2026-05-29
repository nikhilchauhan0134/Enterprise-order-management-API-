using System.Threading.Channels;
using Moq;
using SOPS.Application.Abstractions;
using SOPS.Application.DTOs;
using SOPS.Application.Services;
using SOPS.Domain.Entities;
using SOPS.Domain.Messages;

namespace SOPS.Tests;

public sealed class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    private readonly Mock<IOrderCacheService> _cacheMock = new();
    private readonly FakeTimeProvider _time = new();
    private readonly ConnectivityState _connectivity = new();
    private readonly Channel<OrderMessage> _channel = Channel.CreateUnbounded<OrderMessage>();

    private OrderService CreateSut() =>
        new(_channel, _repoMock.Object, _cacheMock.Object, _connectivity, _time);

    [Fact]
    public async Task Should_AcceptOrder_When_ChannelHasCapacity()
    {
        _connectivity.SetOnline(true);
        var req = new CreateOrderRequest(CustomerId: Guid.NewGuid(), Total: 99.99m);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().SubmitAsync(req, CancellationToken.None);

        Assert.Equal(Domain.Enums.OrderResultStatus.Accepted, result.Status);
        Assert.True(_channel.Reader.TryRead(out _));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_StoreInCache_When_OrderSaved()
    {
        _connectivity.SetOnline(true);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateSut().SubmitAsync(new CreateOrderRequest(Guid.NewGuid(), 50m), CancellationToken.None);

        _cacheMock.Verify(c => c.SetOrderAsync(It.IsAny<OrderDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.InvalidateListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_FallbackToOutbox_When_Offline()
    {
        _connectivity.SetOnline(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.EnqueuePersistentAsync(It.IsAny<OrderMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().SubmitAsync(
            new CreateOrderRequest(Guid.NewGuid(), 10m), CancellationToken.None);

        Assert.Equal(Domain.Enums.OrderResultStatus.Queued, result.Status);
        _repoMock.Verify(r => r.EnqueuePersistentAsync(
            It.IsAny<OrderMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
