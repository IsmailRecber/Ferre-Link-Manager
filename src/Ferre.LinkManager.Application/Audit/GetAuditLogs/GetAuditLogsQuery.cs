namespace Ferre.LinkManager.Application.Audit.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    int? Page,
    int? PageSize,
    string? Action,
    DateTime? FromUtc,
    DateTime? ToUtc);