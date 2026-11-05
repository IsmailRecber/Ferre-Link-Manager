using Ferre.LinkManager.Application.Categories;
using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Authorization;
using Ferre.LinkManager.Application.Common.Exceptions;
using Ferre.LinkManager.Domain.Links;

namespace Ferre.LinkManager.Application.Links.CreateLink;

public sealed class CreateLinkHandler
{
    private readonly ILinkRepository _linkRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLinkHandler(
        ILinkRepository linkRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _linkRepository = linkRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateLinkResponse> HandleAsync(
        CreateLinkCommand command,
        CancellationToken cancellationToken)
    {
        var roles = Roles.Canonicalize(command.Roles)
            ?? throw new ValidationException(new[] { "InvalidRole" });

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken)
            ?? throw new NotFoundException($"Category '{command.CategoryId}' was not found.");

        if (!category.IsActive)
        {
            throw new ConflictException("Links cannot be added to an inactive category.");
        }

        var link = Link.Create(command.Title, command.Url, category.Id, roles);

        await _linkRepository.AddAsync(link, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateLinkResponse(link.Id, link.Title, link.Url.Value, link.CategoryId);
    }
}