using ModularShop.Kernel.Domain.Repositories;

namespace ModularShop.Modules.Support.Domain.Repositories;

/// <summary>
/// The Support module's <b>specific</b> repository. It extends the generic <see cref="IRepository{T}"/>
/// (so it still offers all the usual CRUD/read methods for <see cref="Ticket"/>) and adds one query the
/// generic repository cannot serve well.
/// <para>
/// This is the reason to write a specific repository — the generic one would be both <b>inefficient</b>
/// and the <b>wrong shape</b> for the ticket list: rendering the list needs only a message <i>count</i>,
/// but <c>ListWithIncludes(t =&gt; t.Messages)</c> would load every message body of every ticket just to
/// call <c>.Count</c> on them. <see cref="ListSummariesAsync"/> projects the count in the database
/// instead (see the implementation).
/// </para>
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>Lists ticket summaries (header + message count), most recently opened first.</summary>
    Task<IReadOnlyList<TicketSummary>> ListSummariesAsync(CancellationToken cancellationToken = default);
}
