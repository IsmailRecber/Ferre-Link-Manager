namespace Ferre.LinkManager.Application.Links.GetMyLinks;

public sealed record GetMyLinksQuery(IReadOnlyCollection<string> Roles);