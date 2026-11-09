using System.Diagnostics;
using Ferre.LinkManager.Api;
using Ferre.LinkManager.Api.Features.Categories;
using Ferre.LinkManager.Api.Features.Links;
using Ferre.LinkManager.Api.Features.Users;
using Ferre.LinkManager.Api.Observability;
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

// Reports requests that take longer than half a second. Only the path is logged, never the query string.
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    if (stopwatch.ElapsedMilliseconds > 500)
    {
        app.Logger.LogWarning(
            "Slow request: {Method} {Path} -> {StatusCode} in {ElapsedMs} ms",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
});

// Order matters: errors first, then authentication, then authorization.
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Ferre Link Manager API");
app.MapAuthEndpoints();
app.MapCategoryEndpoints();
app.MapLinkEndpoints();
app.MapUserManagementEndpoints();

app.Run();

public partial class Program { }