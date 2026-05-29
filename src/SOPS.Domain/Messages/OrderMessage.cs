using System.Text.Json;

namespace SOPS.Domain.Messages;

/// <summary>
/// Represents an immutable order message used for asynchronous processing.
///
/// Why Record Instead of Class?
/// - OrderMessage is a data contract (DTO/Event) whose purpose is to carry data.
/// - Records provide value-based equality, making two messages with the same data equal.
/// - Records are immutable by default, preventing accidental changes while the
///   message is being processed by background workers or external systems.
/// - Records reduce boilerplate code compared to traditional classes.
/// - Ideal for queue messages, events, and outbox payloads in distributed systems.
/// </summary>
public sealed record OrderMessage(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    DateTime CreatedAt)
{
    /// <summary>
    /// Creates an OrderMessage from application data.
    /// </summary>
    public static OrderMessage From(
        Guid orderId,
        Guid customerId,
        decimal total,
        DateTime createdAt) =>
        new(orderId, customerId, total, createdAt);

    /// <summary>
    /// Creates a sample message for testing and demo scenarios.
    /// </summary>
    public static OrderMessage Fake() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10m,
            DateTime.UtcNow);

    /// <summary>
    /// Converts the message into JSON format for storage or transmission.
    /// </summary>
    public string ToJson() =>
        JsonSerializer.Serialize(this);

    /// <summary>
    /// Recreates an OrderMessage from a JSON payload.
    /// </summary>
    public static OrderMessage FromJson(string json) =>
        JsonSerializer.Deserialize<OrderMessage>(json)
        ?? throw new InvalidOperationException(
            "Invalid OrderMessage payload.");
}