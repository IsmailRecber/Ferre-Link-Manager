namespace Ferre.LinkManager.Application.Links.SetLinkActive;

public sealed record SetLinkActiveCommand(Guid Id, bool IsActive);