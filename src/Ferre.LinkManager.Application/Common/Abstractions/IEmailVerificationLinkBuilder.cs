namespace Ferre.LinkManager.Application.Common.Abstractions;

public interface IEmailVerificationLinkBuilder
{
    string Build(Guid userId, string token);
}