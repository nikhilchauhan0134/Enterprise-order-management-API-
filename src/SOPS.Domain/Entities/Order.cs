namespace SOPS.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid ExternalId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAt { get; private set; }
    public ICollection<OrderLine> Lines { get; private set; } = new List<OrderLine>();

    private Order() { }

    public static Order Create(Guid customerId, decimal total, DateTime createdAt, Guid? externalId = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId ?? Guid.NewGuid(),
            CustomerId = customerId,
            TotalAmount = total,
            Status = "Pending",
            CreatedAt = createdAt
        };
    }

    public void MarkProcessing() => Status = "Processing";
    public void MarkCompleted() => Status = "Completed";
    public void MarkFailed() => Status = "Failed";
}
