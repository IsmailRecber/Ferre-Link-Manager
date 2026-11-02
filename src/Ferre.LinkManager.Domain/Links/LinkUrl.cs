using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Links;

public sealed class LinkUrl : ValueObject
{
    public const int MaxLength = 2048;

    private LinkUrl(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LinkUrl Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Link URL cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Link URL cannot exceed {MaxLength} characters.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new DomainException("Link URL must be a valid absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainException("Only HTTP and HTTPS links are supported.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new DomainException("Link URL must not contain credentials.");
        }

        return new LinkUrl(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}