using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ms2.Api.Clients;
using Ms2.Api.Data;
using Ms2.Api.Tests.Fakes;

namespace Ms2.Api.Tests;

/// <summary>
/// Startet die API im Speicher für die Tests. Statt der echten Datenbank ist <see cref="FakeTicketRepository"/>
/// registriert, statt MS1 der <see cref="FakeMitarbeiterClient"/>, und Migrationen beim Start sind aus.
/// So laufen die Tests bei allen lokal und in der Pipeline ohne MariaDB und ohne MS1.
/// </summary>
public class Ms2ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Das Fake-Repository, das die API in den Tests verwendet.</summary>
    public FakeTicketRepository Repository { get; } = new();

    /// <summary>Der Fake für MS1, den die API in den Tests verwendet.</summary>
    public FakeMitarbeiterClient MitarbeiterClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Datenbank:MigrationBeimStart", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITicketRepository>();
            services.AddSingleton<ITicketRepository>(Repository);
            services.RemoveAll<IMitarbeiterClient>();
            services.AddSingleton<IMitarbeiterClient>(MitarbeiterClient);
        });
    }
}
