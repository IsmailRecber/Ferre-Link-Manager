namespace Ferre.LinkManager.Application.Links.UpdateLink;

public sealed record UpdateLinkCommand(
    Guid Id,
    string Title,
    string Url,
    Guid CategoryId,
    IReadOnlyCollection<string> Roles);