using Microsoft.AspNetCore.Http;
using ModularShop.SharedKernel.Identity;

namespace ModularShop.SharedKernel.Web;

/// <summary>
/// Lightweight <see cref="ICurrentUser"/> that reads an <c>X-User-Id</c> request header and falls
/// back to a system user. It demonstrates WHERE identity belongs in an MM (the shared kernel)
/// without the ceremony of a full auth stack — swap in JWT/OIDC behind the same interface later.
/// </summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public string UserId =>
        _accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault() is { Length: > 0 } id
            ? id
            : "system";

    public string UserName => UserId;
}
