namespace ModularShop.SharedKernel.Identity;

/// <summary>
/// The current user. Identity/authorization is a cross-cutting concern, so its abstraction lives
/// in the shared kernel (mirroring where the Platform places identity). The implementation here
/// is intentionally lightweight; real JWT/OIDC authentication would plug in behind this interface.
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
    string UserName { get; }
}
