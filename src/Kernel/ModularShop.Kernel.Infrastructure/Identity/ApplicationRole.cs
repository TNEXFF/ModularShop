using Microsoft.AspNetCore.Identity;

namespace ModularShop.Kernel.Infrastructure.Identity;

/// <summary>The application's role (e.g. <c>Admin</c>, <c>Agent</c>), extending Identity's <see cref="IdentityRole"/>.</summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}
