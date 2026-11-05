namespace Ferre.LinkManager.Application.Common.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, User };

  
    public static IReadOnlyCollection<string>? Canonicalize(IEnumerable<string?>? roles)
    {
        var result = new List<string>();

        foreach (var role in roles ?? Enumerable.Empty<string?>())
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            var match = All.FirstOrDefault(known =>
                string.Equals(known, role.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                return null;
            }

            result.Add(match);
        }

        return result;
    }
}