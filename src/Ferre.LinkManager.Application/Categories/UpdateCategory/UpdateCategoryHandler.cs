using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Categories;

namespace Ferre.LinkManager.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Category '{command.Id}' was not found.");

        var newName = CategoryName.Create(command.Name);

        if (newName != category.Name
            && await _categoryRepository.ExistsByNameAsync(newName, cancellationToken))
        {
            throw new ConflictException($"A category named '{newName.Value}' already exists.");
        }

        category.Rename(command.Name);

        if (command.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}