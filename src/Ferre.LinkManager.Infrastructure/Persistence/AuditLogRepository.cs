using Ferre.LinkManager.Application.Audit;
using Ferre.LinkManager.Domain.Audit;

namespace Ferre.LinkManager.Infrastructure.Persistence;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await _db.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}