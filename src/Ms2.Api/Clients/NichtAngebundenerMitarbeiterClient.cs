namespace Ms2.Api.Clients;

/// <summary>
/// Platzhalter, bis der echte HTTP-Client für MS1 da ist (ms2#3). Meldet MS1 immer als nicht erreichbar,
/// damit ohne Prüfung des Mitarbeiters kein Ticket gespeichert wird.
/// </summary>
public class NichtAngebundenerMitarbeiterClient : IMitarbeiterClient
{
    public Task<bool> ExistiertAsync(int mitarbeiterId, CancellationToken cancellationToken = default) =>
        throw new Ms1NichtErreichbarException("Der Client für MS1 ist noch nicht angebunden (ms2#3).");
}
