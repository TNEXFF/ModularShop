namespace ModularShop.Kernel.Application.Abstractions;

/// <summary>
/// Commits the changes staged on the repositories as a single unit (one <c>SaveChanges</c> on the single
/// host context, i.e. one transaction). Keeping commit separate from the repositories means a use case
/// controls the transaction boundary: stage several operations across repositories, then save once.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists all staged changes. Returns the number of state entries written.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
