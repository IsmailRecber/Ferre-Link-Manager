namespace Ferre.LinkManager.Application.Common.Abstractions;

public sealed record RegisterUserResult(bool Succeeded, Guid? UserId, IReadOnlyCollection<string> ErrorCodes);

public sealed record ConfirmEmailResult(bool Succeeded);

public interface IIdentityService
{
 
    Task<RegisterUserResult> RegisterUserAsync(string email, string password, CancellationToken cancellationToken);

    
    Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConfirmEmailResult> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken);
}