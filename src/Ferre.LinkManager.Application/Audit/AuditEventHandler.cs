using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Users.SetUserActive;
using Ferre.LinkManager.Domain.Audit;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Audit;

public sealed class AuditEventHandler :
    IEventHandler<LinkCreated>,
    IEventHandler<LinkUpdated>,
    IEventHandler<LinkActivated>,
    IEventHandler<LinkDeactivated>,
    IEventHandler<LinkDeleted>,
    IEventHandler<CategoryCreated>,
    IEventHandler<CategoryRenamed>,
    IEventHandler<CategoryActivated>,
    IEventHandler<CategoryDeactivated>,
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

    public Task HandleAsync(LinkActivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkActivated, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(LinkDeactivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkDeactivated, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(LinkDeleted e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.LinkDeleted, "Link", e.LinkId, e.Title, e.Url, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(CategoryCreated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.CategoryCreated, "Category", e.CategoryId, e.Name, null, e.OccurredOnUtc, cancellationToken);

    // Both the old and the new name are kept: "Old name → New name".
    public Task HandleAsync(CategoryRenamed e, CancellationToken cancellationToken) =>
        WriteAsync(
            AuditActions.CategoryRenamed, "Category", e.CategoryId, $"{e.OldName} → {e.NewName}", null,
            e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(CategoryActivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.CategoryActivated, "Category", e.CategoryId, e.Name, null, e.OccurredOnUtc, cancellationToken);

    public Task HandleAsync(CategoryDeactivated e, CancellationToken cancellationToken) =>
        WriteAsync(AuditActions.CategoryDeactivated, "Category", e.CategoryId, e.Name, null, e.OccurredOnUtc, cancellationToken);

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
            action,
            entityType,
            entityId,
            Limit(entityName, AuditLog.MaxEntityNameLength),
            Limit(entityUrl, AuditLog.MaxEntityUrlLength),
            _currentUser.UserId,
            occurredOnUtc);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // A value longer than its column would make the database reject the whole record, so it is shortened.
    private static string? Limit(string? value, int maxLength) =>
        value is not null && value.Length > maxLength
            ? value[..(maxLength - 1)] + "…"
            : value;
}