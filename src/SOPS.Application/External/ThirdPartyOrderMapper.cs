using SOPS.Domain.Messages;

namespace SOPS.Application.External;

public static class ThirdPartyOrderMapper
{
    public static ThirdPartyOrderRequestV2 ToV2Request(OrderMessage message) =>
        new(
            message.OrderId,
            message.CustomerId,
            message.Total,
            Currency: "USD",
            SubmittedAt: message.CreatedAt);

    public static ThirdPartyOrderResponse ToV1Response(ThirdPartyOrderResponseV2 v2) =>
        new(v2.Status, v2.ExternalReference);
}
