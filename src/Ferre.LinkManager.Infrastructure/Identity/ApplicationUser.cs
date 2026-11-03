using Microsoft.AspNetCore.Identity;

namespace Ferre.LinkManager.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}