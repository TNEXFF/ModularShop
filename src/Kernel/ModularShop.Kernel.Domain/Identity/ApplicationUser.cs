using Microsoft.AspNetCore.Identity;

namespace ModularShop.Kernel.Domain.Identity;

/// <summary>
/// The application's user, extending ASP.NET Core Identity's <see cref="IdentityUser"/>. Authentication is
/// a cross-cutting concern owned by the <b>kernel</b>. The identity entities live in the kernel's core
/// (domain) layer — a deliberate, documented exception to Clean Architecture (the domain references the
/// Identity base types) so that no layer needs the kernel's <i>Infrastructure</i> just to name the user
/// type. Their tables are owned by the single host context (placed in the <c>kernel</c> schema). Feature
/// modules never see this type — they depend on the kernel's <c>ICurrentUser</c> abstraction instead.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>A friendly name shown in the UI (falls back to the email at registration time).</summary>
    public string DisplayName { get; set; } = default!;
}
