using System.Text;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Ferre.LinkManager.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<RegisterUserResult> RegisterUserAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return new RegisterUserResult(false, null, ToCodes(createResult));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.User);

        if (!roleResult.Succeeded)
        {
            // Never leave an account without a role.
            await _userManager.DeleteAsync(user);
            return new RegisterUserResult(false, null, new[] { "RoleAssignmentFailed" });
        }

        return new RegisterUserResult(true, user.Id, Array.Empty<string>());
    }

    public async Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive || user.EmailConfirmed)
        {
            return null;
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Base64Url keeps the token safe inside a URL.
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive)
        {
            return new ConfirmEmailResult(false);
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return new ConfirmEmailResult(false);
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        return new ConfirmEmailResult(result.Succeeded);
    }

    public async Task<string?> GetPendingConfirmationEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive || user.EmailConfirmed)
        {
            return null;
        }

        return user.Email;
    }

    public async Task<PendingConfirmationUser?> FindPendingConfirmationUserAsync(
    string email, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        return new PendingConfirmationUser(user.Id, user.Email);
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var result = new List<UserSummary>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new UserSummary(
                user.Id,
                user.Email ?? string.Empty,
                user.IsActive,
                user.EmailConfirmed,
                roles.ToArray()));
        }

        return result;
    }

    public async Task<UserStateChange> SetUserActiveAsync(
        Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return UserStateChange.NotFound;
        }

        if (user.IsActive == isActive)
        {
            return UserStateChange.Unchanged;
        }

        user.IsActive = isActive;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Updating the user failed: {string.Join(", ", ToCodes(updateResult))}");
        }

        if (!isActive)
        {
            // A new security stamp makes the user's existing login cookies invalid.
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);

            if (!stampResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Invalidating the user's sessions failed: {string.Join(", ", ToCodes(stampResult))}");
            }
        }

        return UserStateChange.Changed;
    }

    private static IReadOnlyCollection<string> ToCodes(IdentityResult result) =>
        result.Errors.Select(error => error.Code).ToArray();
}