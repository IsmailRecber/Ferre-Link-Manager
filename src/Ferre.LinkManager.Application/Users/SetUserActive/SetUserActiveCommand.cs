namespace Ferre.LinkManager.Application.Users.SetUserActive;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive);