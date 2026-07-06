namespace ModularShop.Kernel.Application;

/// <summary>
/// The current user. Identity/authorization is a cross-cutting concern, so its abstraction lives in
/// the kernel's Application layer where use cases can depend on it. The implementation is provided by
/// the Web layer (from the HTTP request); real JWT/OIDC authentication would plug in behind this
/// same interface. <see cref="UserId"/> is a <see cref="Guid"/> to match every entity key in the system.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string UserName { get; }
}
