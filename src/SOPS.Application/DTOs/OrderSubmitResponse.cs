using SOPS.Application.External;
using SOPS.Domain.Enums;

namespace SOPS.Application.DTOs;

public sealed record OrderSubmitResponse(
    Guid OrderId,
    OrderResultStatus Status,
    OrderDto Order,
    bool Cached,
    ThirdPartyDispatchInfo? ThirdParty);

public sealed record ThirdPartyDispatchInfo(
    bool Attempted,
    bool Succeeded,
    string? Status,
    string? ExternalReference,
    string? Error);
