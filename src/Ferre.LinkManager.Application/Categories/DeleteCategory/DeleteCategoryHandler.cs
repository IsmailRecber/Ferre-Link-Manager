using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Application.Links;

namespace Ferre.LinkManager.Application.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILinkRepository _linkRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(
        ICategoryRepository categoryRepository,
        ILinkRepository linkRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _linkRepository = linkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Category '{command.Id}' was not found.");

        if (await _linkRepository.AnyByCategoryIdAsync(category.Id, cancellationToken))
        {
            throw new ConflictException("Category cannot be deleted while it still has links.");
        }

        category.Delete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}