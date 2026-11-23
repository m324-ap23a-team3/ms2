namespace Ms2.Api.Models;

/// <summary>
/// Status eines Tickets (US3 AK1). Wird in der Datenbank als Text gespeichert,
/// damit die Werte dort lesbar sind und nicht von der Reihenfolge im Enum abhängen.
/// </summary>
public enum TicketStatus
{
    Open,
    InProgress,
    Review,
    Done
}
