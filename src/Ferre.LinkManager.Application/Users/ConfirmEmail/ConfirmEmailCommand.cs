namespace Ferre.LinkManager.Application.Users.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token);