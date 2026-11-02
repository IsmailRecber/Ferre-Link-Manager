namespace Ferre.LinkManager.Application.Links.CreateLink;

public sealed record CreateLinkCommand(
    string Title,
    string Url,
    Guid CategoryId,
    IReadOnlyCollection<string> Roles);