using Ferre.LinkManager.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Ferre.LinkManager.Api.IntegrationTests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public TestApiFactory()
    {
        // Program.cs reads the connection string while it starts, so it must be available before that.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection", TestSettings.GetConnectionString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Tests never send real e-mails, even if SMTP is configured on this machine.
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    internal ApiClient CreateApiClient()
    {
        var http = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,   // we want to see the /go redirect itself
            HandleCookies = false,       // ApiClient manages cookies
            BaseAddress = new Uri("https://localhost") // cookies are Secure
        });

        return new ApiClient(http);
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}