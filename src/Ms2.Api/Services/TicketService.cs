using Ms2.Api.Clients;
using Ms2.Api.Contracts;
using Ms2.Api.Data;
using Ms2.Api.Models;

namespace Ms2.Api.Services;

/// <summary>
/// Geschäftslogik für Tickets: Validierung, Prüfung des Mitarbeiters über MS1 und Umwandlung
/// zwischen Request/Response und Entity. Hängt nur von <see cref="ITicketRepository"/> und
/// <see cref="IMitarbeiterClient"/> ab und ist dadurch ohne Datenbank und ohne MS1 testbar.
/// </summary>
public class TicketService(ITicketRepository repository, IMitarbeiterClient mitarbeiterClient, ILogger<TicketService> logger)
{
    /// <summary>
    /// US3: Prüft die Eingaben, fragt MS1 nach dem Mitarbeiter und speichert das Ticket. Die ID vergibt die Datenbank.
    /// </summary>
    public async Task<ServiceErgebnis<TicketResponse>> ErfassenAsync(
        TicketErfassenRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return ServiceErgebnis<TicketResponse>.Ungueltig(new Dictionary<string, string[]>
            {
                ["body"] = ["Der Request-Body fehlt. Erwartet wird ein JSON-Objekt mit status, titel, beschreibung und mitarbeiterId."],
            });
        }

        var fehler = new Dictionary<string, string[]>();

        var status = PruefeStatus(request.Status, fehler);
        var titel = PruefeTitel(request.Titel, fehler);
        PruefeMitarbeiterId(request.MitarbeiterId, fehler);

        if (fehler.Count > 0)
        {
            return ServiceErgebnis<TicketResponse>.Ungueltig(fehler);
        }

        var mitarbeiterId = request.MitarbeiterId!.Value;

        // MS1 wird erst gefragt, wenn der Rest stimmt (AK6)
        try
        {
            if (!await mitarbeiterClient.ExistiertAsync(mitarbeiterId, cancellationToken))
            {
                fehler["mitarbeiterId"] = [$"Es gibt keinen Mitarbeiter mit der ID {mitarbeiterId}."];
                return ServiceErgebnis<TicketResponse>.Ungueltig(fehler);
            }
        }
        catch (Ms1NichtErreichbarException ex)
        {
            logger.LogError(ex, "Mitarbeiter {MitarbeiterId} konnte nicht über MS1 geprüft werden", mitarbeiterId);
            return ServiceErgebnis<TicketResponse>.Fehler(
                "Der Mitarbeiter konnte nicht geprüft werden, weil MS1 nicht erreichbar ist. Das Ticket wurde nicht gespeichert. Bitte später erneut versuchen.");
        }

        var ticket = new Ticket
        {
            Status = status!.Value,
            Titel = titel!,
            Beschreibung = string.IsNullOrWhiteSpace(request.Beschreibung) ? null : request.Beschreibung.Trim(),
            MitarbeiterId = mitarbeiterId,
        };

        try
        {
            var gespeichert = await repository.HinzufuegenAsync(ticket, cancellationToken);
            return ServiceErgebnis<TicketResponse>.Erfolg(TicketResponse.Aus(gespeichert));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ticket konnte nicht gespeichert werden");
            return ServiceErgebnis<TicketResponse>.Fehler(
                "Das Ticket konnte wegen eines Datenbankfehlers nicht gespeichert werden. Bitte später erneut versuchen.");
        }
    }

    private static TicketStatus? PruefeStatus(string? wert, Dictionary<string, string[]> fehler)
    {
        var erlaubt = string.Join(", ", Enum.GetNames<TicketStatus>());

        if (string.IsNullOrWhiteSpace(wert))
        {
            fehler["status"] = [$"Status ist ein Pflichtfeld. Erlaubt sind: {erlaubt}."];
            return null;
        }

        // «In Progress» wie in der Spezifikation und «InProgress» sind beide erlaubt, Gross-/Kleinschreibung egal.
        // Bewusst kein Enum.TryParse: Das würde auch Zahlen wie «1» annehmen.
        var gesucht = wert.Replace(" ", "");
        foreach (var status in Enum.GetValues<TicketStatus>())
        {
            if (string.Equals(status.ToString(), gesucht, StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }
        }

        fehler["status"] = [$"Status «{wert}» ist unbekannt. Erlaubt sind: {erlaubt}."];
        return null;
    }

    private static string? PruefeTitel(string? wert, Dictionary<string, string[]> fehler)
    {
        var titel = wert?.Trim();

        if (string.IsNullOrEmpty(titel))
        {
            fehler["titel"] = ["Titel ist ein Pflichtfeld."];
            return null;
        }

        if (titel.Length > Ticket.TitelMaxLaenge)
        {
            fehler["titel"] = [$"Titel darf höchstens {Ticket.TitelMaxLaenge} Zeichen lang sein."];
            return null;
        }

        return titel;
    }

    private static void PruefeMitarbeiterId(int? wert, Dictionary<string, string[]> fehler)
    {
        if (wert is null)
        {
            fehler["mitarbeiterId"] = ["Ein Ticket braucht einen Mitarbeiter: mitarbeiterId ist ein Pflichtfeld."];
        }
        else if (wert <= 0)
        {
            fehler["mitarbeiterId"] = [$"mitarbeiterId muss eine positive ganze Zahl sein, war aber {wert}."];
        }
    }
}
