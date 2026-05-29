using SOPS.Domain.Enums;

namespace SOPS.Application.DTOs;

public sealed record OrderResult(Guid OrderId, OrderResultStatus Status)
{
    public static OrderResult Accepted(Guid orderId) => new(orderId, OrderResultStatus.Accepted);
    public static OrderResult Queued(Guid orderId) => new(orderId, OrderResultStatus.Queued);
}
