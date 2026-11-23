using Ms2.Api.Models;

namespace Ms2.Api.Contracts;

/// <summary>
/// Ein Ticket mit allen Details, wie es die API zurückgibt (US3 AK8).
/// </summary>
/// <param name="Id">Eindeutige, automatisch vergebene ID.</param>
/// <param name="Status"><c>Open</c>, <c>InProgress</c>, <c>Review</c> oder <c>Done</c>.</param>
/// <param name="Titel">Titel des Tickets.</param>
/// <param name="Beschreibung">Beschreibungstext oder <c>null</c>.</param>
/// <param name="ReviewDatum">Zeitpunkt in UTC, an dem das Ticket auf Review gesetzt wurde, oder <c>null</c>.</param>
/// <param name="DoneDatum">Zeitpunkt in UTC, an dem das Ticket auf Done gesetzt wurde, oder <c>null</c>.</param>
/// <param name="MitarbeiterId">ID des zugewiesenen Mitarbeiters in MS1.</param>
public record TicketResponse(
    int Id,
    string Status,
    string Titel,
    string? Beschreibung,
    DateTime? ReviewDatum,
    DateTime? DoneDatum,
    int MitarbeiterId)
{
    public static TicketResponse Aus(Ticket ticket) => new(
        ticket.Id,
        ticket.Status.ToString(),
        ticket.Titel,
        ticket.Beschreibung,
        ticket.ReviewDatum,
        ticket.DoneDatum,
        ticket.MitarbeiterId);
}
