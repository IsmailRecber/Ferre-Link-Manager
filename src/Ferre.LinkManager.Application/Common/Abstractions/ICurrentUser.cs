namespace Ferre.LinkManager.Application.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}