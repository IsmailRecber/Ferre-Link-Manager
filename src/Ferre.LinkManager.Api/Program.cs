using Ferre.LinkManager.Infrastructure;
using Ferre.LinkManager.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddInfrastructure(
    connectionString,
    options => builder.Configuration.GetSection(AdminSeedOptions.SectionName).Bind(options));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

app.MapGet("/", () => "Ferre Link Manager API");

app.Run();