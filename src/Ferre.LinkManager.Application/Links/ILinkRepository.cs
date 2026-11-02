using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Links;

public interface ILinkRepository
{
    Task AddAsync(Link link, CancellationToken cancellationToken);

    Task<bool> AnyByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
}