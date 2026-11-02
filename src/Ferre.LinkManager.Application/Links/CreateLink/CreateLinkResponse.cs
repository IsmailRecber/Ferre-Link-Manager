namespace Ferre.LinkManager.Application.Links.CreateLink;

public sealed record CreateLinkResponse(Guid Id, string Title, string Url, Guid CategoryId);