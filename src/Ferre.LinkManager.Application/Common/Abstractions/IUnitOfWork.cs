namespace Ferre.LinkManager.Application.Common.Abstractions;


public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}