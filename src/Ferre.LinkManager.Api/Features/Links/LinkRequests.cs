namespace Ferre.LinkManager.Api.Features.Links;

public sealed record CreateLinkRequest(string Title, string Url, Guid CategoryId, string[] Roles);

public sealed record UpdateLinkRequest(string Title, string Url, Guid CategoryId, string[] Roles);

public sealed record SetLinkActiveRequest(bool IsActive);