# ms2 – Verwaltung von Tickets

Teil von **M324 Projekt 4: Ticketsystem**. Team-Repo: [m324-ap23a-team3/team](https://github.com/m324-ap23a-team3/team)

- Stack: C# / ASP.NET Core Web API (.NET 10), EF Core mit MariaDB (Pomelo), Tests mit xUnit
- Pipeline: `.github/workflows/ci.yml` – Tests bei jedem Push und Pull Request, Image nach `ghcr.io/m324-ap23a-team3/ms2` bei Push auf `main`
- Rechte: Team `devops` (alle Mitglieder, Maintain). `main` ist geschützt: Pull Request mit 1 Approval und grünem Check `test`

## Lokal

Voraussetzungen: .NET 10 SDK, Docker.

```bash
docker compose up -d                  # lokale MariaDB auf Port 3307
dotnet run --project src/Ms2.Api      # wendet Migrationen beim Start an (Development)
dotnet test                           # braucht keine Datenbank
```

Health-Check: `GET /health`

## API

| Endpunkt | Beschreibung |
|---|---|
| `POST /api/tickets` | US3: Ticket erfassen. `status` (`Open`, `InProgress`, `Review`, `Done`), `titel` und `mitarbeiterId` sind Pflicht, `beschreibung` ist optional. `201` mit dem Ticket, `400` mit einer Meldung pro Feld, `503` wenn MS1 oder die Datenbank nicht erreichbar ist |

```bash
curl -X POST localhost:5217/api/tickets -H 'Content-Type: application/json'   -d '{"status":"Open","titel":"Drucker defekt","beschreibung":"2. Stock","mitarbeiterId":1}'
```

Der Mitarbeiter wird über MS1 geprüft. Bis der HTTP-Client aus ms2#3 da ist, antwortet der Endpunkt ausserhalb der Tests mit `503`. Review- und Done-Datum folgen mit ms2#5.

## Architektur

Endpoints → `TicketService` → `ITicketRepository` (EF Core/MariaDB) und `IMitarbeiterClient` (MS1). Die Tests ersetzen beide durch Fakes und laufen ohne Datenbank und ohne MS1.

## Datenbank

| | |
|---|---|
| DB | MariaDB 11.4, eigene Instanz nur für MS2 (`docker-compose.yml`). Host-Port 3307, damit sie neben der DB von MS1 (3306) läuft |
| Tabelle | `Tickets`: `Id` (auto), `Status` (Text: `Open`, `InProgress`, `Review`, `Done`; Check-Constraint), `Titel` (max. 200), `Beschreibung` (optional), `ReviewDatum` und `DoneDatum` (UTC, optional), `MitarbeiterId` |
| Mitarbeiter | `MitarbeiterId` ist die ID aus MS1, ohne Fremdschlüssel. MS2 greift nie auf die DB von MS1 zu, geprüft wird über dessen Endpunkt (ms2#3) |
| Connection-String | nur über Konfiguration: `ConnectionStrings:Ms2Db` (lokal in `appsettings.Development.json`, sonst Umgebungsvariable `ConnectionStrings__Ms2Db`) |
| Migrationen | `src/Ms2.Api/Data/Migrations`. Beim Start angewendet, wenn `Datenbank:MigrationBeimStart=true` |

Neue Migration erstellen:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Ms2.Api --output-dir Data/Migrations
```

Das Passwort in `docker-compose.yml` und `appsettings.Development.json` gilt nur für die lokale Entwicklungs-DB. Eigene Werte lassen sich über eine `.env`-Datei (`MS2_DB_PASSWORT`, `MS2_DB_ROOT_PASSWORT`, `MS2_DB_PORT`) setzen; die Datei ist in `.gitignore`. Wer den Port ändert, passt auch den Connection-String an.
