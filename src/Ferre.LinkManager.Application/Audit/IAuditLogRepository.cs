using Ferre.LinkManager.Domain.Audit;

namespace Ferre.LinkManager.Application.Audit;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken);
}