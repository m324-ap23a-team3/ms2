using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ms2.Api.Clients;
using Ms2.Api.Data;
using Ms2.Api.Tests.Fakes;

namespace Ms2.Api.Tests;

public class ArchitekturTests(Ms2ApiFactory factory) : IClassFixture<Ms2ApiFactory>
{
    /// <summary>
    /// In den Tests löst die API Repository und MS1-Client zu den Fakes auf.
    /// Damit braucht kein Test eine Datenbank oder ein laufendes MS1.
    /// </summary>
    [Fact]
    public void Tests_verwenden_Fakes_statt_Datenbank_und_MS1()
    {
        using var scope = factory.Services.CreateScope();

        Assert.IsType<FakeTicketRepository>(scope.ServiceProvider.GetRequiredService<ITicketRepository>());
        Assert.IsType<FakeMitarbeiterClient>(scope.ServiceProvider.GetRequiredService<IMitarbeiterClient>());
    }

    /// <summary>
    /// Ausserhalb der Tests ist die EF-Core-Implementierung des Repositories registriert.
    /// </summary>
    [Fact]
    public void Ohne_Testkonfiguration_ist_EF_Repository_registriert()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddMs2Datenbank(configuration);

        var registrierung = Assert.Single(services, s => s.ServiceType == typeof(ITicketRepository));
        Assert.Equal(typeof(TicketRepository), registrierung.ImplementationType);
    }
}
