namespace Ms2.Api.Contracts;

/// <summary>
/// Request-Body für <c>POST /api/tickets</c> (US3).
/// Alle Felder sind nullable, damit fehlende Felder im Service mit einer verständlichen Meldung
/// pro Feld abgelehnt werden statt mit einem allgemeinen JSON-Fehler.
/// </summary>
/// <param name="Status">Pflicht: <c>Open</c>, <c>InProgress</c> (auch «In Progress»), <c>Review</c> oder <c>Done</c>.</param>
/// <param name="Titel">Pflicht, höchstens 200 Zeichen.</param>
/// <param name="Beschreibung">Beschreibungstext, optional.</param>
/// <param name="MitarbeiterId">Pflicht: ID eines Mitarbeiters aus MS1.</param>
public record TicketErfassenRequest(
    string? Status,
    string? Titel,
    string? Beschreibung,
    int? MitarbeiterId);
