using Ferre.LinkManager.Application.Common.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ferre.LinkManager.Infrastructure.Identity;

public sealed class IdentitySeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminSeedOptions> options,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in new[] { Roles.Admin, Roles.User })
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Creating role '{roleName}' failed: {DescribeErrors(roleResult)}");
            }
        }

        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning(
                "Admin seeding skipped: 'AdminSeed:Email' and 'AdminSeed:Password' are not configured.");
            return;
        }

        if (await _userManager.FindByEmailAsync(_options.Email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _options.Email,
            Email = _options.Email,
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(admin, _options.Password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Admin seeding failed: {DescribeErrors(createResult)}");
        }

        var roleAssignResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);

        if (!roleAssignResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Assigning the Admin role failed: {DescribeErrors(roleAssignResult)}");
        }

        _logger.LogInformation("Initial admin user was created.");
    }

    // Only error codes are reported. Never include the email or password in messages.
    private static string DescribeErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(error => error.Code));
}