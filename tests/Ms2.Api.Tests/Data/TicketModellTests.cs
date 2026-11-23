using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Ms2.Api.Data;
using Ms2.Api.Models;

namespace Ms2.Api.Tests.Data;

/// <summary>
/// Prüft das EF-Core-Modell des Tickets. Das Modell wird nur im Speicher aufgebaut,
/// es wird keine Verbindung zur Datenbank geöffnet.
/// </summary>
public class TicketModellTests
{
    private static IEntityType TicketModell()
    {
        var options = new DbContextOptionsBuilder<Ms2DbContext>()
            .UseMySql("Server=nicht-verwendet", DatenbankExtensions.ServerVersion)
            .Options;
        using var db = new Ms2DbContext(options);

        return db.Model.FindEntityType(typeof(Ticket))!;
    }

    /// <summary>
    /// Happy Path: Die Tabelle enthält genau die Felder aus dem Akzeptanzkriterium,
    /// Titel und Mitarbeiter sind Pflicht, Beschreibung und die beiden Daten sind optional.
    /// </summary>
    [Fact]
    public void Ticket_hat_alle_Felder_mit_passender_Pflicht()
    {
        var ticket = TicketModell();

        var pflicht = ticket.GetProperties().ToDictionary(p => p.Name, p => !p.IsNullable);

        Assert.Equal(
            new Dictionary<string, bool>
            {
                ["Id"] = true,
                ["Status"] = true,
                ["Titel"] = true,
                ["Beschreibung"] = false,
                ["ReviewDatum"] = false,
                ["DoneDatum"] = false,
                ["MitarbeiterId"] = true,
            },
            pflicht);
    }

    /// <summary>
    /// Happy Path: Jeder der vier Statuswerte wird als sein Name gespeichert (z. B. «InProgress»)
    /// und passt in die Spalte.
    /// </summary>
    [Theory]
    [InlineData(TicketStatus.Open, "Open")]
    [InlineData(TicketStatus.InProgress, "InProgress")]
    [InlineData(TicketStatus.Review, "Review")]
    [InlineData(TicketStatus.Done, "Done")]
    public void Status_wird_als_Text_gespeichert(TicketStatus status, string erwartet)
    {
        var spalte = TicketModell().FindProperty(nameof(Ticket.Status))!;

        Assert.Equal(typeof(string), spalte.GetProviderClrType());
        Assert.Equal(erwartet, status.ToString());
        Assert.True(erwartet.Length <= spalte.GetMaxLength());
        Assert.Equal(4, Enum.GetValues<TicketStatus>().Length);
    }

    /// <summary>
    /// Happy Path: Ein aus der Datenbank gelesenes Datum hat keine Zeitzone und wird als UTC markiert,
    /// ohne dass sich die Uhrzeit verschiebt.
    /// </summary>
    [Theory]
    [InlineData(nameof(Ticket.ReviewDatum))]
    [InlineData(nameof(Ticket.DoneDatum))]
    public void Gelesenes_Datum_ist_UTC(string feld)
    {
        var converter = TicketModell().FindProperty(feld)!.GetValueConverter()!;
        var ausDerDb = new DateTime(2026, 11, 23, 9, 30, 0, DateTimeKind.Unspecified);

        var gelesen = (DateTime)converter.ConvertFromProvider(ausDerDb)!;

        Assert.Equal(DateTimeKind.Utc, gelesen.Kind);
        Assert.Equal(ausDerDb.Ticks, gelesen.Ticks);
    }

    /// <summary>
    /// Grenzfall: Ein Datum in lokaler Zeit wird vor dem Speichern nach UTC umgerechnet,
    /// damit in der Datenbank nie eine lokale Zeit steht.
    /// </summary>
    [Fact]
    public void Lokales_Datum_wird_als_UTC_gespeichert()
    {
        var converter = TicketModell().FindProperty(nameof(Ticket.DoneDatum))!.GetValueConverter()!;
        var lokal = new DateTime(2026, 11, 23, 9, 30, 0, DateTimeKind.Local);

        var gespeichert = (DateTime)converter.ConvertToProvider(lokal)!;

        Assert.Equal(lokal.ToUniversalTime(), gespeichert);
    }
}
