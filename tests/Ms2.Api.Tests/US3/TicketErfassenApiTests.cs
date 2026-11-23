using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Ms2.Api.Contracts;
using Ms2.Api.Models;

namespace Ms2.Api.Tests.US3;

/// <summary>
/// API-Tests für US3 <c>POST /api/tickets</c>: prüfen den ganzen HTTP-Weg (Statuscode, JSON-Format,
/// Fehlermeldungen). Die API läuft im Speicher (<see cref="Ms2ApiFactory"/>), Datenbank und MS1 sind durch Fakes ersetzt.
/// Die vollständigen Unit Tests mit Testdoku folgen in ms2#6.
/// </summary>
public class TicketErfassenApiTests : IClassFixture<Ms2ApiFactory>
{
    private const string Route = "/api/tickets";
    private const int VorhandenerMitarbeiter = 7;
    private readonly Ms2ApiFactory _factory;
    private readonly HttpClient _client;

    public TicketErfassenApiTests(Ms2ApiFactory factory)
    {
        _factory = factory;
        _factory.Repository.Zuruecksetzen();
        _factory.MitarbeiterClient.Zuruecksetzen(VorhandenerMitarbeiter);
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Happy Path (AK1–3, 6, 8): Ein gültiges Ticket mit einem Mitarbeiter, den es in MS1 gibt, ergibt
    /// <c>201 Created</c>, und die Antwort enthält das Ticket mit der vergebenen ID und allen Details.
    /// </summary>
    [Fact]
    public async Task Post_gueltig_liefert_201_mit_allen_Details()
    {
        var response = await _client.PostAsJsonAsync(Route, new
        {
            status = "Open",
            titel = "Drucker defekt",
            beschreibung = "Der Drucker im 2. Stock druckt nicht.",
            mitarbeiterId = VorhandenerMitarbeiter,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(
            new TicketResponse(1, "Open", "Drucker defekt", "Der Drucker im 2. Stock druckt nicht.", null, null, VorhandenerMitarbeiter),
            ticket);
        Assert.Single(_factory.Repository.Gespeichert);
    }

    /// <summary>
    /// Happy Path (AK1): Alle vier Statuswerte werden angenommen, auch in der Schreibweise der Spezifikation
    /// («In Progress») und unabhängig von Gross-/Kleinschreibung. Die Beschreibung darf fehlen (AK3).
    /// </summary>
    [Theory]
    [InlineData("Open", TicketStatus.Open)]
    [InlineData("InProgress", TicketStatus.InProgress)]
    [InlineData("In Progress", TicketStatus.InProgress)]
    [InlineData("review", TicketStatus.Review)]
    [InlineData("Done", TicketStatus.Done)]
    public async Task Post_nimmt_alle_vier_Statuswerte_an(string status, TicketStatus erwartet)
    {
        var response = await _client.PostAsJsonAsync(Route, new { status, titel = "Test", mitarbeiterId = VorhandenerMitarbeiter });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(erwartet.ToString(), ticket!.Status);
        Assert.Null(ticket.Beschreibung);
    }

    /// <summary>
    /// Sad Path (AK6): Gibt es den Mitarbeiter in MS1 nicht, kommt <c>400 Bad Request</c> als ProblemDetails
    /// mit einer Meldung beim Feld <c>mitarbeiterId</c>. Es wird nichts gespeichert.
    /// </summary>
    [Fact]
    public async Task Post_mit_unbekanntem_Mitarbeiter_liefert_400_mit_Meldung_beim_Feld()
    {
        var response = await _client.PostAsJsonAsync(Route, new { status = "Open", titel = "Test", mitarbeiterId = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("keinen Mitarbeiter mit der ID 99", Assert.Single(problem!.Errors["mitarbeiterId"]));
        Assert.Empty(_factory.Repository.Gespeichert);
    }

    /// <summary>
    /// Sad Path (AK1, 2, 6): Fehlen alle Felder, enthält die 400-Antwort für jedes Pflichtfeld eine Meldung
    /// (die Beschreibung ist optional). MS1 wird dafür gar nicht erst gefragt.
    /// </summary>
    [Fact]
    public async Task Post_leeres_Objekt_liefert_400_mit_allen_Pflichtfeldern()
    {
        var response = await _client.PostAsJsonAsync(Route, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["mitarbeiterId", "status", "titel"], problem!.Errors.Keys.Order());
        Assert.Equal(0, _factory.MitarbeiterClient.Aufrufe);
        Assert.Empty(_factory.Repository.Gespeichert);
    }

    /// <summary>
    /// Sad Path (AK1): Ein unbekannter Status, auch eine Zahl als Text, ergibt <c>400</c>
    /// mit den erlaubten Werten in der Meldung.
    /// </summary>
    [Theory]
    [InlineData("Geschlossen")]
    [InlineData("1")]
    public async Task Post_mit_unbekanntem_Status_liefert_400_mit_erlaubten_Werten(string status)
    {
        var response = await _client.PostAsJsonAsync(Route, new { status, titel = "Test", mitarbeiterId = VorhandenerMitarbeiter });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Open, InProgress, Review, Done", Assert.Single(problem!.Errors["status"]));
    }

    /// <summary>
    /// Sad Path: Ein falscher Datentyp (<c>"mitarbeiterId": "sieben"</c>) oder kaputtes JSON ergibt
    /// <c>400</c> mit einer Meldung beim betroffenen Feld statt eines leeren Fehlers.
    /// </summary>
    [Theory]
    [InlineData("""{"status":"Open","titel":"Test","mitarbeiterId":"sieben"}""", "mitarbeiterId")]
    [InlineData("{kaputt", "body")]
    public async Task Post_mit_unlesbarem_Request_liefert_400_mit_Meldung(string json, string feld)
    {
        var response = await _client.PostAsync(Route, new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(feld, problem!.Errors.Keys);
    }

    /// <summary>
    /// Sad Path (AK6): Ist MS1 nicht erreichbar, kann der Mitarbeiter nicht geprüft werden. Die API antwortet
    /// mit <c>503</c> und einer Meldung, und es wird nichts gespeichert.
    /// </summary>
    [Fact]
    public async Task Post_bei_nicht_erreichbarem_MS1_liefert_503_und_speichert_nichts()
    {
        _factory.MitarbeiterClient.NichtErreichbar = true;

        var response = await _client.PostAsJsonAsync(Route, new { status = "Open", titel = "Test", mitarbeiterId = VorhandenerMitarbeiter });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("MS1 nicht erreichbar", problem!.Detail);
        Assert.Empty(_factory.Repository.Gespeichert);
    }

    /// <summary>
    /// Sad Path: Fällt die Datenbank beim Speichern aus, antwortet die API mit <c>503</c>
    /// und einer Meldung ohne technische Details.
    /// </summary>
    [Fact]
    public async Task Post_bei_Datenbankfehler_liefert_503()
    {
        _factory.Repository.Fehler = new InvalidOperationException("Unable to connect to any of the specified MySQL hosts.");

        var response = await _client.PostAsJsonAsync(Route, new { status = "Open", titel = "Test", mitarbeiterId = VorhandenerMitarbeiter });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Datenbankfehler", problem!.Detail);
        Assert.DoesNotContain("MySQL", problem.Detail);
    }
}
