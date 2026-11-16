using Ferre.LinkManager.Application.Common.Abstractions;
using Ferre.LinkManager.Application.Common.Exceptions;

namespace Ferre.LinkManager.Application.Links.SetLinkActive;

public sealed class SetLinkActiveHandler
{
    private readonly ILinkRepository _linkRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetLinkActiveHandler(ILinkRepository linkRepository, IUnitOfWork unitOfWork)
    {
        _linkRepository = linkRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(SetLinkActiveCommand command, CancellationToken cancellationToken)
    {
        var link = await _linkRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Link '{command.Id}' was not found.");

        if (link.IsActive == command.IsActive)
        {
            return;
        }

        if (command.IsActive)
        {
            link.Activate();
        }
        else
        {
            link.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}