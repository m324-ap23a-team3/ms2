namespace Ms2.Api.Models;

/// <summary>
/// Ticket, das einem Mitarbeiter aus MS1 zugewiesen ist (US3).
/// </summary>
public class Ticket
{
    /// <summary>Maximale Länge des Titels (Spaltenlänge in der DB).</summary>
    public const int TitelMaxLaenge = 200;

    /// <summary>Eindeutige ID, wird von der Datenbank automatisch vergeben.</summary>
    public int Id { get; set; }

    /// <summary>Open, InProgress, Review oder Done (US3 AK1).</summary>
    public TicketStatus Status { get; set; }

    public required string Titel { get; set; }

    /// <summary>Beschreibungstext, optional und ohne Längenbegrenzung (US3 AK3).</summary>
    public string? Beschreibung { get; set; }

    /// <summary>Zeitpunkt in UTC, an dem das Ticket in den Status Review gesetzt wurde (US3 AK4).</summary>
    public DateTime? ReviewDatum { get; set; }

    /// <summary>Zeitpunkt in UTC, an dem das Ticket in den Status Done gesetzt wurde (US3 AK5).</summary>
    public DateTime? DoneDatum { get; set; }

    /// <summary>
    /// ID des zugewiesenen Mitarbeiters in MS1 (US3 AK6). Kein Fremdschlüssel: Die Mitarbeiter liegen
    /// in der Datenbank von MS1 und werden nur über dessen Endpunkt geprüft.
    /// </summary>
    public int MitarbeiterId { get; set; }
}
