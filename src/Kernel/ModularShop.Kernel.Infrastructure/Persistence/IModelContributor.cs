using Microsoft.EntityFrameworkCore;

namespace ModularShop.Kernel.Infrastructure.Persistence;

/// <summary>
/// Implemented by every context that contributes its slice of the model to the single host context —
/// each feature module's context and the kernel's. The host asks each contributor to stamp its entities
/// onto the one shared <see cref="ModelBuilder"/>, which is how "a context per module" composes into a
/// single runtime model that owns the migrations. The shim just surfaces the context's own protected
/// <c>OnModelCreating</c> so the host can invoke it.
/// </summary>
public interface IModelContributor
{
    void ApplyModel(ModelBuilder modelBuilder);
}
