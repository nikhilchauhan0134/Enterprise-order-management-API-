using SOPS.Application.DTOs;
using SOPS.Domain.Entities;

namespace SOPS.Application.Mapping;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) =>
        new(order.Id, order.ExternalId, order.CustomerId, order.TotalAmount, order.Status, order.CreatedAt);
}
