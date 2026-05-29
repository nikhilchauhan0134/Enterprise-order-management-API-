namespace SOPS.Application.DTOs;

public sealed record OrderExportFilter(DateTime? From = null, DateTime? To = null);
