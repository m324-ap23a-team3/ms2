using Ms2.Api.Clients;

namespace Ms2.Api.Tests.Fakes;

/// <summary>
/// Ersetzt MS1 in den Tests: kennt eine feste Menge von Mitarbeiter-IDs.
/// Mit <see cref="NichtErreichbar"/> lässt sich ein Ausfall von MS1 simulieren.
/// </summary>
public class FakeMitarbeiterClient : IMitarbeiterClient
{
    /// <summary>IDs der Mitarbeiter, die es in «MS1» gibt.</summary>
    public HashSet<int> VorhandeneIds { get; } = [];

    /// <summary>Wenn <c>true</c>, verhält sich der Fake wie ein nicht erreichbares MS1.</summary>
    public bool NichtErreichbar { get; set; }

    /// <summary>Wie oft MS1 gefragt wurde.</summary>
    public int Aufrufe { get; private set; }

    /// <summary>Setzt den Fake zurück: MS1 ist erreichbar und kennt nur die übergebenen IDs.</summary>
    public void Zuruecksetzen(params int[] vorhandeneIds)
    {
        VorhandeneIds.Clear();
        VorhandeneIds.UnionWith(vorhandeneIds);
        NichtErreichbar = false;
        Aufrufe = 0;
    }

    public Task<bool> ExistiertAsync(int mitarbeiterId, CancellationToken cancellationToken = default)
    {
        Aufrufe++;

        return NichtErreichbar
            ? Task.FromException<bool>(new Ms1NichtErreichbarException("MS1 ist nicht erreichbar (simuliert)."))
            : Task.FromResult(VorhandeneIds.Contains(mitarbeiterId));
    }
}
