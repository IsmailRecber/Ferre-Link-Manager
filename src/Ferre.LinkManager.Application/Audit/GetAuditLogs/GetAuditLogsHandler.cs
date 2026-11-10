using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Application.Audit.GetAuditLogs;

public sealed class GetAuditLogsHandler
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    private const int MaxPage = 100_000;

    private readonly IReadDbContext _db;
    private readonly IIdentityService _identityService;

    public GetAuditLogsHandler(IReadDbContext db, IIdentityService identityService)
    {
        _db = db;
        _identityService = identityService;
    }

    public async Task<AuditLogPage> HandleAsync(GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? DefaultPageSize;
        var action = string.IsNullOrWhiteSpace(query.Action) ? null : query.Action.Trim();

        var errors = new List<string>();

        if (page < 1 || page > MaxPage)
        {
            errors.Add("InvalidPage");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            errors.Add("InvalidPageSize");
        }

        if (action is not null && !AuditActions.All.Contains(action))
        {
            errors.Add("InvalidAction");
        }

        if (query.FromUtc is not null && query.ToUtc is not null && query.FromUtc > query.ToUtc)
        {
            errors.Add("InvalidDateRange");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        IQueryable<AuditLog> logs = _db.AuditLogs;

        if (action is not null)
        {
            logs = logs.Where(log => log.Action == action);
        }

        if (query.FromUtc is DateTime fromUtc)
        {
            logs = logs.Where(log => log.OccurredOnUtc >= fromUtc);
        }

        if (query.ToUtc is DateTime toUtc)
        {
            logs = logs.Where(log => log.OccurredOnUtc <= toUtc);
        }

        var totalCount = await logs.CountAsync(cancellationToken);

        var rows = await logs
            .OrderByDescending(log => log.OccurredOnUtc)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new
            {
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.EntityName,
                log.EntityUrl,
                log.UserId,
                log.OccurredOnUtc
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // E-mail addresses are not stored in the audit log. They are looked up now, for this page only.
        var userIds = rows
            .Where(row => row.UserId.HasValue)
            .Select(row => row.UserId!.Value)
            .Concat(rows.Where(row => row.EntityType == "User").Select(row => row.EntityId))
            .Distinct()
            .ToArray();

        var emails = await _identityService.GetEmailsAsync(userIds, cancellationToken);

        var items = rows
            .Select(row => new AuditLogDto(
                row.Id,
                row.Action,
                row.EntityType,
                row.EntityId,
                row.EntityType == "User"
                    ? (emails.TryGetValue(row.EntityId, out var target) ? target : null)
                    : row.EntityName,
                row.EntityUrl,
                row.UserId,
                row.UserId.HasValue && emails.TryGetValue(row.UserId.Value, out var actor) ? actor : null,
                // The database returns the time without a time zone. Marking it as UTC makes the JSON end
                // with "Z", so the browser shows the user's local time instead of treating it as local.
                DateTime.SpecifyKind(row.OccurredOnUtc, DateTimeKind.Utc)))
            .ToList();

        return new AuditLogPage(items, totalCount, page, pageSize);
    }
}