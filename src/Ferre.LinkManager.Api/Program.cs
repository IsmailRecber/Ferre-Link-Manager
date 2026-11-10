using Ferre.LinkManager.Api;
using Ferre.LinkManager.Api.Features.Audit;
using Ferre.LinkManager.Api.Features.Categories;
using Ferre.LinkManager.Api.Features.Links;
using Ferre.LinkManager.Api.Features.Users;
using Ferre.LinkManager.Api.Observability;
using Ferre.LinkManager.Api.Security;
using Ferre.LinkManager.Application;
using Ferre.LinkManager.Infrastructure;
using Ferre.LinkManager.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddInfrastructure(
    connectionString,
    options => builder.Configuration.GetSection(AdminSeedOptions.SectionName).Bind(options),
    options => builder.Configuration.GetSection(FrontendOptions.SectionName).Bind(options),
    options => builder.Configuration.GetSection(SmtpOptions.SectionName).Bind(options));

builder.Services.AddApplication();
builder.Services.AddApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

// Security headers go first, so even error answers carry them.
app.UseSecurityHeaders();

// Order matters: errors first, then files, then rate limiting, authentication and authorization.
app.UseExceptionHandler();
app.UseHttpsRedirection();

var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // index.html names the current script files, so it must never be cached.
        if (string.Equals(context.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store";
        }
    }
};

// Serves the Angular files from wwwroot (they exist only after "ng build").
app.UseDefaultFiles();
app.UseStaticFiles(staticFileOptions);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCategoryEndpoints();
app.MapLinkEndpoints();
app.MapUserManagementEndpoints();
app.MapAuditEndpoints();

// An unknown API address must answer 404, not the Angular page.
app.MapFallback("/api/{**path}", () => Results.NotFound());

// Any other address that is not a file is an Angular page (for example /verify-email): serve the app.
app.MapFallbackToFile("index.html", staticFileOptions);

app.Run();

public partial class Program { }