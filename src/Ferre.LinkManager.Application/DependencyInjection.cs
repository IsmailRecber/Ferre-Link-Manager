using Ferre.LinkManager.Application.Audit;
using Ferre.LinkManager.Application.Categories.CreateCategory;
using Ferre.LinkManager.Application.Categories.DeleteCategory;
using Ferre.LinkManager.Application.Categories.GetCategories;
using Ferre.LinkManager.Application.Categories.UpdateCategory;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Links.CreateLink;
using Ferre.LinkManager.Application.Links.DeleteLink;
using Ferre.LinkManager.Application.Links.GetAllLinks;
using Ferre.LinkManager.Application.Links.GetLinkRedirect;
using Ferre.LinkManager.Application.Links.GetMyLinks;
using Ferre.LinkManager.Application.Links.UpdateLink;
using Ferre.LinkManager.Application.Users.ConfirmEmail;
using Ferre.LinkManager.Application.Users.GetUsers;
using Ferre.LinkManager.Application.Users.Register;
using Ferre.LinkManager.Application.Users.ResendVerificationEmail;
using Ferre.LinkManager.Application.Users.SendVerificationEmail;
using Ferre.LinkManager.Application.Users.SetUserActive;
using Ferre.LinkManager.Domain.Categories;
using Ferre.LinkManager.Domain.Links;
using Microsoft.Extensions.DependencyInjection;

namespace Ferre.LinkManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeleteCategoryHandler>();
        services.AddScoped<GetCategoriesHandler>();

        services.AddScoped<CreateLinkHandler>();
        services.AddScoped<UpdateLinkHandler>();
        services.AddScoped<DeleteLinkHandler>();
        services.AddScoped<GetMyLinksHandler>();
        services.AddScoped<GetAllLinksHandler>();
        services.AddScoped<GetLinkRedirectHandler>();

        services.AddScoped<RegisterHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<ResendVerificationEmailHandler>();
        services.AddScoped<GetUsersHandler>();
        services.AddScoped<SetUserActiveHandler>();

        // One class listens to several events, so each event type points to the same instance.
        services.AddScoped<AuditEventHandler>();
        services.AddScoped<IEventHandler<LinkCreated>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<LinkUpdated>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<LinkDeleted>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<CategoryCreated>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<CategoryDeleted>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<UserActivated>>(sp => sp.GetRequiredService<AuditEventHandler>());
        services.AddScoped<IEventHandler<UserDeactivated>>(sp => sp.GetRequiredService<AuditEventHandler>());

        services.AddScoped<SendVerificationEmailHandler>();
        services.AddScoped<IEventHandler<UserRegistered>>(sp => sp.GetRequiredService<SendVerificationEmailHandler>());

        return services;
    }
}