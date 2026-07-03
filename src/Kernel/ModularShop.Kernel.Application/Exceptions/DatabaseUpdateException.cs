namespace ModularShop.Kernel.Application.Exceptions;

/// <summary>
/// Thrown by the unit of work when committing changes fails (for example, a concurrency conflict). It
/// wraps the underlying persistence exception so the Application layer can surface a stable error type
/// without referencing EF Core.
/// </summary>
public sealed class DatabaseUpdateException : Exception
{
    public DatabaseUpdateException(string message, Exception innerException)
        : base(message, innerException) { }
}
