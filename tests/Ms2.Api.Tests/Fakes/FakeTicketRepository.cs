using Ms2.Api.Data;
using Ms2.Api.Models;

namespace Ms2.Api.Tests.Fakes;

/// <summary>
/// Ersetzt die Datenbank in den Tests: hält die Tickets in einer Liste und vergibt IDs selbst.
/// Mit <see cref="Fehler"/> lässt sich ein Datenbankfehler simulieren.
/// </summary>
public class FakeTicketRepository : ITicketRepository
{
    private readonly List<Ticket> _tickets = [];
    private readonly Lock _lock = new();
    private int _naechsteId = 1;

    /// <summary>Wenn gesetzt, schlägt jeder Zugriff mit dieser Exception fehl (z. B. «Datenbank nicht erreichbar»).</summary>
    public Exception? Fehler { get; set; }

    /// <summary>Alle bisher gespeicherten Tickets.</summary>
    public IReadOnlyList<Ticket> Gespeichert
    {
        get { lock (_lock) { return [.. _tickets]; } }
    }

    /// <summary>Setzt Daten und Fehler zurück, damit jeder Test mit einem leeren Zustand startet.</summary>
    public void Zuruecksetzen()
    {
        lock (_lock)
        {
            _tickets.Clear();
            _naechsteId = 1;
            Fehler = null;
        }
    }

    public Task<Ticket> HinzufuegenAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (Fehler is not null)
            {
                return Task.FromException<Ticket>(Fehler);
            }

            ticket.Id = _naechsteId++;
            _tickets.Add(ticket);
            return Task.FromResult(ticket);
        }
    }
}
