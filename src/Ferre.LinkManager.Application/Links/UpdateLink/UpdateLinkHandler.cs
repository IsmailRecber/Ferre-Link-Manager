using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Ferre.LinkManager.Application.Common.Exceptions;

namespace Ferre.LinkManager.Application.Links.UpdateLink;

public sealed class UpdateLinkHandler
{
    private readonly ILinkRepository _linkRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLinkHandler(
        ILinkRepository linkRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _linkRepository = linkRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(UpdateLinkCommand command, CancellationToken cancellationToken)
    {
        var roles = Roles.Canonicalize(command.Roles)
            ?? throw new ValidationException(new[] { "InvalidRole" });

        var link = await _linkRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Link '{command.Id}' was not found.");

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken)
            ?? throw new NotFoundException($"Category '{command.CategoryId}' was not found.");

        if (link.CategoryId != category.Id && !category.IsActive)
        {
            throw new ConflictException("Links cannot be moved to an inactive category.");
        }

        link.Update(command.Title, command.Url, category.Id, roles);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}