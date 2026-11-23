using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ms2.Api.Tests;

/// <summary>
/// Startet die API im Speicher für die Tests. Migrationen beim Start sind ausgeschaltet,
/// damit die Tests ohne laufende Datenbank funktionieren.
/// </summary>
public class Ms2ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Datenbank:MigrationBeimStart", "false");
    }
}
