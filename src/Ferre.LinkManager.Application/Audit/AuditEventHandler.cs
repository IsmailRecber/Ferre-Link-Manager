using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Users.SetUserActive;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Audit;

public sealed class AuditEventHandler :
    IEventHandler<LinkCreated>,
    IEventHandler<LinkUpdated>,
    IEventHandler<LinkDeleted>,
    IEventHandler<CategoryCreated>,
    IEventHandler<CategoryDeleted>,
    IEventHandler<UserActivated>,
    IEventHandler<UserDeactivated>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AuditEventHandler(
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public Task HandleAsync(LinkCreated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkCreated, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(LinkUpdated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkUpdated, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(LinkDeleted e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkDeleted, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(CategoryCreated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.CategoryCreated, "Category", e.CategoryId, e.Name, null, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(CategoryDeleted e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.CategoryDeleted, "Category", e.CategoryId, e.Name, null, e.OccurredOnUtc, cancellationToken);

    // User events carry no name or e-mail on purpose: only the user id is stored.
    public Task HandleAsync(UserActivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.UserActivated, "User", e.UserId, null, null, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(UserDeactivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.UserDeactivated, "User", e.UserId, null, null, e.OccurredOnUtc, cancellationToken);

    private async Task WriteAsync(
        string action,
        string entityType,
        Guid entityId,
        string? entityName,
        string? entityUrl,
        DateTime occurredOnUtc,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            action, entityType, entityId, entityName, entityUrl, _currentUser.UserId, occurredOnUtc);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}