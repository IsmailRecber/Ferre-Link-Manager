using Ferre.LinkManager.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace Ferre.LinkManager.Infrastructure.Identity;

public sealed class EmailVerificationLinkBuilder : IEmailVerificationLinkBuilder
{
    private readonly FrontendOptions _options;

    public EmailVerificationLinkBuilder(IOptions<FrontendOptions> options)
    {
        _options = options.Value;
    }

    public string Build(Guid userId, string token)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";
    }
}