using Ferre.LinkManager.Application.Common.Abstractions;

namespace Ferre.LinkManager.Application.Users.GetUsers;

public sealed class GetUsersHandler
{
    private readonly IIdentityService _identityService;

    public GetUsersHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<IReadOnlyList<UserSummary>> HandleAsync(CancellationToken cancellationToken) =>
        _identityService.GetUsersAsync(cancellationToken);
}