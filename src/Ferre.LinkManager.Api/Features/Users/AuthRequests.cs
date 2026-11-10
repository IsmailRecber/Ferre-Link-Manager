namespace Ferre.LinkManager.Api.Features.Users;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record ConfirmEmailRequest(Guid UserId, string Token);

public sealed record ResendVerificationEmailRequest(Guid UserId);

public sealed record ResendVerificationByEmailRequest(string Email);

public sealed record MeResponse(Guid? UserId, string? Email, string[] Roles);