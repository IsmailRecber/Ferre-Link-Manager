namespace Ferre.LinkManager.Application.Links.GetAllLinks;

public sealed record AdminLinkDto(
    Guid Id,
    string Title,
    string Url,
    Guid CategoryId,
    string CategoryName,
    bool IsActive,
    IReadOnlyCollection<string> Roles);