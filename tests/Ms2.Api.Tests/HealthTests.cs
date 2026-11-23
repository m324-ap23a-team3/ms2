using System.Net;

namespace Ms2.Api.Tests;

public class HealthTests(Ms2ApiFactory factory) : IClassFixture<Ms2ApiFactory>
{
    /// <summary>
    /// Der Health-Endpunkt antwortet mit 200, auch ohne laufende Datenbank.
    /// </summary>
    [Fact]
    public async Task Health_liefert_200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
