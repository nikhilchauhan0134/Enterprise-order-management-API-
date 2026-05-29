namespace SOPS.Application.DTOs;

public sealed record OrderExportDto(Guid Id, string Status, decimal Total, DateTime CreatedAt);
