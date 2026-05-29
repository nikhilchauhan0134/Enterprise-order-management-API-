namespace SOPS.Domain.Entities;

public sealed class OutboxOrder
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
