using Ferre.LinkManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.MapGet("/", () => "Ferre Link Manager API");

app.Run();