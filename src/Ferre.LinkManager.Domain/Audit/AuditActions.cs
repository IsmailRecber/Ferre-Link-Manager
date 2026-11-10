namespace Ferre.LinkManager.Domain.Audit;

public static class AuditActions
{
    public const string LinkCreated = "Link.Created";
    public const string LinkUpdated = "Link.Updated";
    public const string LinkActivated = "Link.Activated";
    public const string LinkDeactivated = "Link.Deactivated";
    public const string LinkDeleted = "Link.Deleted";
    public const string CategoryCreated = "Category.Created";
    public const string CategoryRenamed = "Category.Renamed";
    public const string CategoryActivated = "Category.Activated";
    public const string CategoryDeactivated = "Category.Deactivated";
    public const string CategoryDeleted = "Category.Deleted";
    public const string UserActivated = "User.Activated";
    public const string UserDeactivated = "User.Deactivated";

    /// <summary>Every action the audit log can contain. Used to validate filters.</summary>
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        LinkCreated,
        LinkUpdated,
        LinkActivated,
        LinkDeactivated,
        LinkDeleted,
        CategoryCreated,
        CategoryRenamed,
        CategoryActivated,
        CategoryDeactivated,
        CategoryDeleted,
        UserActivated,
        UserDeactivated
    };
}