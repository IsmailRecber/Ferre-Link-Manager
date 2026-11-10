using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Links;

public sealed class Link : AggregateRoot
{
    public const int MaxTitleLength = 200;

    private readonly List<LinkRoleAccess> _roleAccesses = new();

    private Link(Guid id, string title, LinkUrl url, Guid categoryId) : base(id)
    {
        Title = title;
        Url = url;
        CategoryId = categoryId;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public LinkUrl Url { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedOnUtc { get; }
    public IReadOnlyCollection<LinkRoleAccess> RoleAccesses => _roleAccesses.AsReadOnly();

    public static Link Create(string? title, string? url, Guid categoryId, IEnumerable<string?>? roleNames)
    {
        var link = new Link(
            Guid.NewGuid(),
            ValidateTitle(title),
            LinkUrl.Create(url),
            EnsureCategoryId(categoryId));

        link.ReplaceRoles(roleNames);
        link.RaiseDomainEvent(new LinkCreated(link.Id, link.Title, link.Url.Value));
        return link;
    }

    public void Update(string? title, string? url, Guid categoryId, IEnumerable<string?>? roleNames)
    {
        EnsureNotDeleted();

        var newTitle = ValidateTitle(title);
        var newUrl = LinkUrl.Create(url);
        var newCategoryId = EnsureCategoryId(categoryId);

        ReplaceRoles(roleNames);

        Title = newTitle;
        Url = newUrl;
        CategoryId = newCategoryId;

        RaiseDomainEvent(new LinkUpdated(Id, Title, Url.Value));
    }

    public void Activate()
    {
        EnsureNotDeleted();

        // Nothing changed, so nothing is recorded.
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        RaiseDomainEvent(new LinkActivated(Id, Title, Url.Value));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RaiseDomainEvent(new LinkDeactivated(Id, Title, Url.Value));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        RaiseDomainEvent(new LinkDeleted(Id, Title, Url.Value));
    }

    public bool IsAccessibleTo(IEnumerable<string> userRoles) =>
        _roleAccesses.Any(access =>
            userRoles.Contains(access.RoleName, StringComparer.OrdinalIgnoreCase));

    private void ReplaceRoles(IEnumerable<string?>? roleNames)
    {
        var normalized = (roleNames ?? Enumerable.Empty<string?>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new DomainException("A link must have at least one permitted role.");
        }

        if (normalized.Any(role => role.Length > LinkRoleAccess.MaxRoleNameLength))
        {
            throw new DomainException(
                $"Role name cannot exceed {LinkRoleAccess.MaxRoleNameLength} characters.");
        }

        _roleAccesses.Clear();

        foreach (var role in normalized)
        {
            _roleAccesses.Add(LinkRoleAccess.Create(Id, role));
        }
    }

    private static string ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Link title cannot be empty.");
        }

        var trimmed = title.Trim();

        if (trimmed.Length > MaxTitleLength)
        {
            throw new DomainException($"Link title cannot exceed {MaxTitleLength} characters.");
        }

        return trimmed;
    }

    private static Guid EnsureCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category id cannot be empty.");
        }

        return categoryId;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException("Link is deleted.");
        }
    }
}