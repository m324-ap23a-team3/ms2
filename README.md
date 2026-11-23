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
