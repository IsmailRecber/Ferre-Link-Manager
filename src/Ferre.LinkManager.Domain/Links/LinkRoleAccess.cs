using Ferre.LinkManager.Domain.Common;

namespace Ferre.LinkManager.Domain.Links;

public sealed class LinkRoleAccess : Entity
{
    public const int MaxRoleNameLength = 256;

    private LinkRoleAccess(Guid id, Guid linkId, string roleName) : base(id)
    {
        LinkId = linkId;
        RoleName = roleName;
    }

    public Guid LinkId { get; }
    public string RoleName { get; }

    internal static LinkRoleAccess Create(Guid linkId, string roleName) =>
        new(Guid.NewGuid(), linkId, roleName);
}