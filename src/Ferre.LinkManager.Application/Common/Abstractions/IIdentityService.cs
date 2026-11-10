namespace Ferre.LinkManager.Application.Common.Abstractions;

public sealed record RegisterUserResult(bool Succeeded, Guid? UserId, IReadOnlyCollection<string> ErrorCodes);

public sealed record ConfirmEmailResult(bool Succeeded);

public sealed record PendingConfirmationUser(Guid UserId, string Email);

public sealed record UserSummary(
    Guid Id,
    string Email,
    bool IsActive,
    bool EmailConfirmed,
    IReadOnlyCollection<string> Roles);

public enum UserStateChange
{
    NotFound,
    Unchanged,
    Changed
}

public interface IIdentityService
{
    /// <summary>
    /// Creates a user with the standard User role. The role is never chosen by the caller.
    /// </summary>
    Task<RegisterUserResult> RegisterUserAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Returns null when the user does not exist, is inactive or is already confirmed.
    /// </summary>
    Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConfirmEmailResult> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the e-mail of an active, not yet confirmed user. Returns null in every other case.
    /// </summary>
    Task<string?> GetPendingConfirmationEmailAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an active, not yet confirmed user by e-mail address. Returns null in every other case.
    /// </summary>
    Task<PendingConfirmationUser?> FindPendingConfirmationUserAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Activates or deactivates a user. Deactivating also invalidates the user's existing sessions.
    /// </summary>
    Task<UserStateChange> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
}