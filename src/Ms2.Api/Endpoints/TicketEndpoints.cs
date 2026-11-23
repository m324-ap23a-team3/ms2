using Microsoft.AspNetCore.Http.HttpResults;
using Ms2.Api.Contracts;
using Ms2.Api.Services;

namespace Ms2.Api.Endpoints;

/// <summary>
/// HTTP-Schicht für Tickets. Enthält keine Logik, sondern übersetzt nur
/// zwischen HTTP und <see cref="TicketService"/>.
/// </summary>
public static class TicketEndpoints
{
    public const string Route = "/api/tickets";

    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var gruppe = app.MapGroup(Route).WithTags("Tickets");

        gruppe.MapPost("", ErfassenAsync)
            .WithName("TicketErfassen")
            .WithSummary("US3: Ticket erfassen")
            .WithDescription(
                "Erfasst ein neues Ticket und weist es einem Mitarbeiter zu. Status, Titel und mitarbeiterId sind Pflicht, " +
                "die Beschreibung ist optional. Der Mitarbeiter wird über MS1 geprüft. Die ID vergibt der Service automatisch. " +
                "Bei ungültigen Eingaben kommt 400 mit einer Meldung pro Feld in `errors`, ist MS1 oder die Datenbank nicht erreichbar, 503.")
            .Accepts<TicketErfassenRequest>("application/json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    /// <summary>US3: Ticket erfassen.</summary>
    internal static async Task<Results<Created<TicketResponse>, ValidationProblem, ProblemHttpResult>> ErfassenAsync(
        TicketErfassenRequest? request,
        TicketService service,
        CancellationToken cancellationToken)
    {
        var ergebnis = await service.ErfassenAsync(request, cancellationToken);

        return ergebnis.Art switch
        {
            // Kein Location-Header: Es gibt keinen Endpunkt für ein einzelnes Ticket (offene Frage team#20).
            ErgebnisArt.Erfolg => TypedResults.Created((string?)null, ergebnis.Wert),
            ErgebnisArt.Ungueltig => TypedResults.ValidationProblem(
                ergebnis.Validierungsfehler.ToDictionary(),
                title: "Die Eingaben sind ungültig.",
                detail: "Bitte die markierten Felder korrigieren."),
            _ => TypedResults.Problem(
                ergebnis.Fehlermeldung,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dienst nicht verfügbar"),
        };
    }
}
