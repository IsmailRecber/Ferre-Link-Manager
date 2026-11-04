using Ferre.LinkManager.Application.Audit;
using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Links;
using Ferre.LinkManager.Infrastructure.Events;
using Ferre.LinkManager.Infrastructure.Identity;
using Ferre.LinkManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ferre.LinkManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<AdminSeedOptions>? configureAdminSeed = null,
        Action<FrontendOptions>? configureFrontend = null,
        Action<SmtpOptions>? configureSmtp = null)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<AdminSeedOptions>(options => configureAdminSeed?.Invoke(options));
        services.AddScoped<IdentitySeeder>();

        services.Configure<FrontendOptions>(options => configureFrontend?.Invoke(options));
        services.Configure<SmtpOptions>(options => configureSmtp?.Invoke(options));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailVerificationLinkBuilder, EmailVerificationLinkBuilder>();

        // If SMTP is configured, real e-mails are sent. Otherwise the console fallback is used.
        services.AddSingleton<ConsoleEmailSender>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(sp =>
        {
            var smtp = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;

            return string.IsNullOrWhiteSpace(smtp.Host)
                ? sp.GetRequiredService<ConsoleEmailSender>()
                : sp.GetRequiredService<SmtpEmailSender>();
        });

        return services;
    }
}