using Ferre.LinkManager.Application.Common.Abstractions;

namespace Ferre.LinkManager.Infrastructure.Identity;


public sealed class ConsoleEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        Console.WriteLine("----- DEVELOPMENT E-MAIL (not sent) -----");
        Console.WriteLine($"To: {to}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine(body);
        Console.WriteLine("-----------------------------------------");

        return Task.CompletedTask;
    }
}