using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ModularShop.Kernel.Application;

namespace ModularShop.Kernel.Web;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the authenticated ASP.NET Core principal (cookie auth, set by
/// the kernel's Identity sign-in). Use cases depend only on the kernel's <c>ICurrentUser</c> abstraction
/// and never touch <c>HttpContext</c> or Identity directly. Falls back to <c>"system"</c> when there is
/// no authenticated user (e.g. during startup seeding).
/// </summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public string UserName =>
        Principal?.Identity?.Name
        ?? Principal?.FindFirstValue(ClaimTypes.Email)
        ?? "system";
}
