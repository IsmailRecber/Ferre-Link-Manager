using System.Text;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

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

    private static IReadOnlyCollection<string> ToCodes(IdentityResult result) =>
        result.Errors.Select(error => error.Code).ToArray();
}