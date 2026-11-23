using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ms2.Api.Clients;
using Ms2.Api.Data;
using Ms2.Api.Endpoints;
using Ms2.Api.Fehlerbehandlung;
using Ms2.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Zahlen nur als JSON-Zahl annehmen ("mitarbeiterId": 7), nicht als Text ("mitarbeiterId": "7"), wie in MS1
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddMs2Datenbank(builder.Configuration);
builder.Services.AddScoped<TicketService>();
// Platzhalter, bis der HTTP-Client für MS1 registriert ist (ms2#3). TryAdd, damit dessen Registrierung gewinnt.
builder.Services.TryAddScoped<IMitarbeiterClient, NichtAngebundenerMitarbeiterClient>();

// Fehler immer als ProblemDetails (RFC 9457) mit verständlicher Meldung
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
// Ungültiges JSON auch ausserhalb von Development als Exception melden, damit ApiExceptionHandler
// eine Meldung pro Feld zurückgeben kann (sonst nur ein leeres 400)
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

var app = builder.Build();

await app.MigriereDatenbankFallsAktiviertAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health-Endpunkt fuer Smoke Tests und Monitoring (Ops)
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ms2" }));

app.MapTicketEndpoints();

app.Run();

// Damit WebApplicationFactory<Program> in den Tests darauf zugreifen kann
public partial class Program;
