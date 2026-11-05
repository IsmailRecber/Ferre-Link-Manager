namespace Ferre.LinkManager.Application.Links.GetLinkRedirect;

public sealed record GetLinkRedirectQuery(Guid Id, IReadOnlyCollection<string> Roles);