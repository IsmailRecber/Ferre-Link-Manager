namespace Ferre.LinkManager.Application.Audit.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid EntityId,
    string? EntityName,
    string? EntityUrl,
    Guid? UserId,
    string? UserEmail,
    DateTime OccurredOnUtc);

public sealed record AuditLogPage(
    IReadOnlyList<AuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize);