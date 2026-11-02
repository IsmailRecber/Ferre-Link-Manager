namespace Ferre.LinkManager.Application.Links;

public interface ILinkRepository
{
    Task<bool> AnyByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken);
}