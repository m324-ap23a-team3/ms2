using Ms2.Api.Models;

namespace Ms2.Api.Data;

/// <summary>
/// Implementierung von <see cref="ITicketRepository"/> mit EF Core und MariaDB.
/// Enthält keine Geschäftslogik, nur Datenzugriff.
/// </summary>
public class TicketRepository(Ms2DbContext db) : ITicketRepository
{
    public async Task<Ticket> HinzufuegenAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}
