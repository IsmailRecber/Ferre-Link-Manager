using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;

namespace Ferre.LinkManager.Application.Links.DeleteLink;

public sealed class DeleteLinkHandler
{
    private readonly ILinkRepository _linkRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLinkHandler(ILinkRepository linkRepository, IUnitOfWork unitOfWork)
    {
        _linkRepository = linkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteLinkCommand command, CancellationToken cancellationToken)
    {
        var link = await _linkRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Link '{command.Id}' was not found.");

        link.Delete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}