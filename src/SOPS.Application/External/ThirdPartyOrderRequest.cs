namespace SOPS.Application.External;

/// <summary>
/// Represents the request payload sent to the external order processing API.
///
/// This record defines the contract expected by the third-party service and is
/// serialized into JSON when making an HTTP POST request.
///
/// Example JSON payload:
/// {
///   "orderId": "123",
///   "customerId": "456",
///   "totalAmount": 1000
/// }
///
/// Why use a record?
/// - Acts as a lightweight Data Transfer Object (DTO).
/// - Provides immutable properties through init-only setters.
/// - Supports value-based equality.
/// - Reduces boilerplate code compared to a traditional class.
/// - Clearly separates external API contracts from domain models.
///
/// Note:
/// This type contains no business logic. Its sole responsibility is to define
/// the structure of data exchanged with the third-party API.
/// </summary>
public sealed record ThirdPartyOrderRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount);