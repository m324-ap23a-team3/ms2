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

            // Wiederholt Verbindungsfehler, z. B. wenn der DB-Container beim Start noch nicht bereit ist
            options.UseMySql(connectionString, ServerVersion, mysql => mysql.EnableRetryOnFailure());
        });

        return services;
    }

    /// <summary>
    /// Wendet beim Start ausstehende Migrationen an, wenn <c>Datenbank:MigrationBeimStart</c> auf <c>true</c> steht
    /// (lokal und im Container). In den Tests ist es aus.
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
