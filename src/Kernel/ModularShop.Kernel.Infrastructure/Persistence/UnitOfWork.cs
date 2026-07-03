using Microsoft.EntityFrameworkCore;
using ModularShop.Kernel.Application.Abstractions;
using ModularShop.Kernel.Application.Exceptions;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Commits the changes staged on the repositories by calling <c>SaveChanges</c> on the single host
/// <see cref="DbContext"/>. Translates the EF Core concurrency exception into the Application layer's
/// <see cref="DatabaseUpdateException"/> so callers never have to reference EF Core to handle it.
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
            throw new DatabaseUpdateException(
                "A concurrency conflict occurred while saving changes to the database. See the inner exception.", ex);
        }
    }
}
