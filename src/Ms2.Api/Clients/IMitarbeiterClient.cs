namespace Ms2.Api.Clients;

/// <summary>
/// Zugriff auf die Mitarbeiter von MS1. MS2 liest sie nur über den Endpunkt von MS1 (US2),
/// nie aus dessen Datenbank. In den Unit Tests ersetzt ein Fake den Aufruf.
/// </summary>
public interface IMitarbeiterClient
{
    /// <summary>
    /// Prüft, ob in MS1 ein Mitarbeiter mit dieser ID existiert.
    /// </summary>
    /// <exception cref="Ms1NichtErreichbarException">MS1 antwortet nicht oder mit einem Fehler.</exception>
    Task<bool> ExistiertAsync(int mitarbeiterId, CancellationToken cancellationToken = default);
}

/// <summary>MS1 ist nicht erreichbar oder hat mit einem Fehler geantwortet. Die API antwortet darauf mit 503.</summary>
public class Ms1NichtErreichbarException(string message, Exception? innerException = null)
    : Exception(message, innerException);
