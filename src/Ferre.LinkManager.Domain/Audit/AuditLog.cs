using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Audit;

public sealed class AuditLog : Entity
{
    public const int MaxActionLength = 100;
    public const int MaxEntityTypeLength = 50;
    public const int MaxEntityNameLength = 200;
    public const int MaxEntityUrlLength = 2048;

    private AuditLog(
        Guid id,
        string action,
        string entityType,
        Guid entityId,
        string? entityName,
        string? entityUrl,
        Guid? userId,
        DateTime occurredOnUtc) : base(id)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        EntityName = entityName;
        EntityUrl = entityUrl;
        UserId = userId;
        OccurredOnUtc = occurredOnUtc;
    }

    public string Action { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public string? EntityName { get; }
    public string? EntityUrl { get; }
    public Guid? UserId { get; }
    public DateTime OccurredOnUtc { get; }

    public static AuditLog Create(
        string action,
        string entityType,
        Guid entityId,
        string? entityName,
        string? entityUrl,
        Guid? userId,
        DateTime occurredOnUtc)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > MaxActionLength)
        {
            throw new DomainException("Audit action is invalid.");
        }

        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > MaxEntityTypeLength)
        {
            throw new DomainException("Audit entity type is invalid.");
        }

        if (entityId == Guid.Empty)
        {
            throw new DomainException("Audit entity id cannot be empty.");
        }

        return new AuditLog(
            Guid.NewGuid(), action, entityType, entityId, entityName, entityUrl, userId, occurredOnUtc);
    }
}