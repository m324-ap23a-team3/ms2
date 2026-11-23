using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ms2.Api.Fehlerbehandlung;

/// <summary>
/// Wandelt Exceptions in verständliche ProblemDetails-Antworten um:
/// <list type="bullet">
/// <item>Ungültiges JSON oder falscher Datentyp (z. B. <c>"mitarbeiterId": "sieben"</c>) → 400 mit Meldung pro Feld (US3 AK9)</item>
/// <item>Alles andere → 500 ohne technische Details</item>
/// </list>
/// </summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    private static readonly Dictionary<string, string> MeldungProFeld = new(StringComparer.OrdinalIgnoreCase)
    {
        ["status"] = "Status muss ein Text sein: Open, InProgress, Review oder Done.",
        ["titel"] = "Titel muss ein Text sein.",
        ["beschreibung"] = "Beschreibung muss ein Text sein.",
        ["mitarbeiterid"] = "mitarbeiterId muss eine ganze Zahl sein.",
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            problem = new HttpValidationProblemDetails(FehlerAus(badRequest))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Die Eingaben sind ungültig.",
                Detail = "Der Request konnte nicht gelesen werden. Bitte die markierten Felder korrigieren.",
            };
        }
        else
        {
            logger.LogError(exception, "Unerwarteter Fehler");
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unerwarteter Fehler",
                Detail = "Es ist ein unerwarteter Fehler aufgetreten. Bitte später erneut versuchen.",
            };
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static Dictionary<string, string[]> FehlerAus(BadHttpRequestException exception)
    {
        if (exception.InnerException is JsonException { Path: { } pfad } && pfad.StartsWith("$.", StringComparison.Ordinal))
        {
            // Feldname wie im JSON (camelCase), damit er zu den Meldungen des Service passt
            var feld = char.ToLowerInvariant(pfad[2]) + pfad[3..];
            var meldung = MeldungProFeld.GetValueOrDefault(feld, $"Das Feld {feld} hat einen ungültigen Wert.");
            return new() { [feld] = [meldung] };
        }

        return new() { ["body"] = ["Der Request-Body ist kein gültiges JSON-Objekt."] };
    }
}
