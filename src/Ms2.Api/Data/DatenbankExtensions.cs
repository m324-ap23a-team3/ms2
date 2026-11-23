using Microsoft.EntityFrameworkCore;

namespace Ms2.Api.Data;

public static class DatenbankExtensions
{
    /// <summary>Name des Connection-Strings in der Konfiguration (Umgebungsvariable: <c>ConnectionStrings__Ms2Db</c>).</summary>
    public const string ConnectionStringName = "Ms2Db";

    /// <summary>
    /// MariaDB-Version, gegen die EF Core SQL erzeugt. Fest eingetragen statt <c>ServerVersion.AutoDetect</c>,
    /// damit die App (und die Tests) ohne laufende Datenbank starten.
    /// </summary>
    public static readonly MariaDbServerVersion ServerVersion = new(new Version(11, 4));

    /// <summary>
    /// Registriert den <see cref="Ms2DbContext"/>. Der Connection-String kommt nur aus der Konfiguration
    /// (appsettings, Umgebungsvariablen, Secrets) und steht nie im Code.
    /// </summary>
    public static IServiceCollection AddMs2Datenbank(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<Ms2DbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection-String '{ConnectionStringName}' fehlt. Setze ihn in appsettings oder als Umgebungsvariable ConnectionStrings__{ConnectionStringName}.");

            // Wiederholt kurze Verbindungsfehler. Bewusst knapp (3 Versuche, max. 5 s) wie in MS1, damit ein Client
            // bei einer ausgefallenen DB nach Sekunden eine 503-Meldung bekommt statt nach über einer Minute.
            options.UseMySql(connectionString, ServerVersion, mysql =>
                mysql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null));
        });

        services.AddScoped<ITicketRepository, TicketRepository>();

        return services;
    }

    /// <summary>
    /// Wendet beim Start ausstehende Migrationen an, wenn <c>Datenbank:MigrationBeimStart</c> auf <c>true</c> steht
    /// (lokal in <c>appsettings.Development.json</c>; im Container über die Umgebungsvariable
    /// <c>Datenbank__MigrationBeimStart=true</c>). In den Tests ist es aus.
    /// </summary>
    public static async Task MigriereDatenbankFallsAktiviertAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Datenbank:MigrationBeimStart"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Ms2DbContext>();
        await db.Database.MigrateAsync();
    }
}
