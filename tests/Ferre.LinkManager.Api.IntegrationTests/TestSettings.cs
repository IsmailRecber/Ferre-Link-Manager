using Microsoft.Extensions.Configuration;

namespace Ferre.LinkManager.Api.IntegrationTests;

internal static class TestSettings
{
    public static string GetConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestSettings).Assembly)
            .Build();

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in the test project's user secrets.");
    }
}