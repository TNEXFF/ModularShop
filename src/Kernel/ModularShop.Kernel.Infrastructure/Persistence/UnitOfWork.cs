using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Application.Exceptions;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Commits the changes staged on the repositories by calling <c>SaveChanges</c> on the single host
/// <see cref="DbContext"/>. Translates EF Core's update exceptions — both the concurrency conflict and
/// the more common constraint failure (unique index, foreign key, NOT NULL) — into the Application
/// layer's <see cref="DatabaseUpdateException"/> so callers never have to reference EF Core to handle them.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;

    public UnitOfWork(DbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Must be caught before the base DbUpdateException below (it derives from it).
            throw new DatabaseUpdateException(
                "A concurrency conflict occurred while saving changes to the database. See the inner exception.", ex);
        }
        catch (DbUpdateException ex)
        {
            // The common save failure: a unique-index, foreign-key or NOT NULL constraint violation.
            throw new DatabaseUpdateException(
                "A database constraint was violated while saving changes (for example a duplicate value or a " +
                "missing related row). See the inner exception.", ex);
        }
    }
}
