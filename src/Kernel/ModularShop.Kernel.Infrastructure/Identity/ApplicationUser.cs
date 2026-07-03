using Microsoft.AspNetCore.Identity;

namespace ModularShop.Kernel.Infrastructure.Identity;

/// <summary>
/// The application's user, extending ASP.NET Core Identity's <see cref="IdentityUser"/>. Authentication
/// is a cross-cutting concern, so Identity lives in the <b>kernel</b> and its tables are owned by the
/// single host context (placed in the <c>kernel</c> schema). Modules never see this type — they depend
/// on the kernel's <c>ICurrentUser</c> abstraction instead.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>A friendly name shown in the UI (falls back to the email at registration time).</summary>
    public string DisplayName { get; set; } = default!;
}
