using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Categories;

namespace Ferre.LinkManager.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCategoryResponse> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = Category.Create(command.Name);

        if (await _categoryRepository.ExistsByNameAsync(category.Name, cancellationToken))
        {
            throw new ConflictException($"A category named '{category.Name.Value}' already exists.");
        }

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateCategoryResponse(category.Id, category.Name.Value);
    }
}