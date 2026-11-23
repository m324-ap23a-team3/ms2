using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ms2.Api.Models;

namespace Ms2.Api.Data;

/// <summary>
/// Datenbank von MS2. Enthält nur Tickets; Mitarbeiter liest MS2 über den Endpunkt von MS1, nie aus dessen Datenbank.
/// </summary>
public class Ms2DbContext(DbContextOptions<Ms2DbContext> options) : DbContext(options)
{
    /// <summary>Spaltenlänge für den Status, reicht für den längsten Enum-Namen (<c>InProgress</c>).</summary>
    public const int StatusMaxLaenge = 20;

    /// <summary>
    /// MariaDB speichert <c>datetime</c> ohne Zeitzone. Der Converter schreibt immer UTC
    /// und markiert gelesene Werte als UTC, damit sie in der API mit <c>Z</c> ausgegeben werden.
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        wert => wert.Kind == DateTimeKind.Local ? wert.ToUniversalTime() : wert,
        wert => DateTime.SpecifyKind(wert, DateTimeKind.Utc));

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            // Zweite Absicherung neben dem Enum: In der Spalte stehen nur die vier bekannten Statuswerte
            entity.ToTable("Tickets", table => table.HasCheckConstraint(
                "CK_Tickets_Status",
                $"`Status` IN ({string.Join(", ", Enum.GetNames<TicketStatus>().Select(name => $"'{name}'"))})"));

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedOnAdd();
            entity.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(StatusMaxLaenge);
            entity.Property(t => t.Titel).IsRequired().HasMaxLength(Ticket.TitelMaxLaenge);
            entity.Property(t => t.Beschreibung);
            entity.Property(t => t.ReviewDatum).HasConversion(UtcConverter);
            entity.Property(t => t.DoneDatum).HasConversion(UtcConverter);
            entity.Property(t => t.MitarbeiterId).IsRequired();
        });
    }
}
