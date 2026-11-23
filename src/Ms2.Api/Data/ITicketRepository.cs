using Ms2.Api.Models;

namespace Ms2.Api.Data;

/// <summary>
/// Zugriff auf die gespeicherten Tickets. Die Service-Schicht kennt nur dieses Interface,
/// nicht EF Core. In den Unit Tests ersetzt eine Fake-Implementierung die Datenbank.
/// </summary>
public interface ITicketRepository
{
    /// <summary>Speichert ein neues Ticket und gibt es mit der vergebenen ID zurück.</summary>
    Task<Ticket> HinzufuegenAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
