# ms2 – Verwaltung von Tickets

Teil von **M324 Projekt 4: Ticketsystem**. Team-Repo: [m324-ap23a-team3/team](https://github.com/m324-ap23a-team3/team)

- Stack: C# / ASP.NET Core Web API (.NET 10), Tests mit xUnit
- Pipeline: `.github/workflows/ci.yml` – Tests bei jedem Push und Pull Request, Image nach `ghcr.io/m324-ap23a-team3/ms2` bei Push auf `main`
- Code Owner: Team `dev` (Maintain); Team `ops` hat Write (Branch + Pull Request)

## Lokal

```bash
dotnet test
dotnet run --project src/Ms2.Api
docker build -t ms2 . && docker run -p 8080:8080 ms2
```

Health-Check: `GET /health`
