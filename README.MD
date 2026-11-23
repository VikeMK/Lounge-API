# Lounge-API

## Running locally with Docker (recommended)

Runs the whole stack in containers: the API, SQL Server, Azurite (local Azure Blob Storage) and an optional web database UI. You don't need .NET or SQL Server installed.

### Requirements
- [Docker](https://docs.docker.com/get-docker/) with Compose v2 (Docker Desktop on Windows/macOS, or Docker Engine on Linux/WSL)

### First-time setup
1. Copy the example environment file:

       cp .env.example .env

2. Edit `.env` if you want different values (see [Configuration](#configuration)). The defaults work for local development.
   Set `SA_PASSWORD` **before the first run**: SQL Server only reads it when the database volume is created.

### Start
    docker compose up --build

This starts the services in order: `db` waits until SQL Server is healthy, `migrate` applies the EF Core migrations and exits, then `web` starts. `--build` rebuilds the app image so code changes are picked up; add `-d` to run in the background.

| URL | What |
|---|---|
| http://localhost:5000 | The site |
| http://localhost:5000/swagger | API docs (Development only) |
| http://localhost:8978 | CloudBeaver database UI (Development only) |
| `localhost,1433` | SQL Server, for SSMS / VS Code mssql extension (user `sa`, password from `.env`, trust server certificate) |

### Useful commands
| Command | What it does |
|---|---|
| `docker compose ps` | Show service status (`migrate` showing `Exited (0)` is normal) |
| `docker compose logs -f web` | Follow the app's logs |
| `docker compose logs migrate` | See migration output if `web` doesn't start |
| `docker compose down` | Stop and remove containers (data is kept) |
| `docker compose down -v` | Also delete volumes: fresh, empty database next time |

### Seeding test data
A freshly migrated database has no players, so the leaderboard shows a "Maintenance in Progress" banner (it appears whenever there are no players with a country for that game/season).

[`dev/seed-players.sql`](dev/seed-players.sql) inserts a snapshot of the top 50 players from each live leaderboard (mk8dx, mkworld12p, mkworld24p) into the current seasons from `appsettings.json`. It's safe to run more than once. Seeded players have a fake `MKCId` (>= 900000) and no registry link.

Run it in CloudBeaver (paste it into a SQL editor and use **Execute script**, Alt+X, not Ctrl+Enter) or from the terminal:

    docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<SA_PASSWORD>' -C < dev/seed-players.sql

The app picks up database changes within about 5 seconds; no restart needed.

### Using CloudBeaver
1. Open http://localhost:8978 and complete the setup wizard. The admin user name can't be `admin` (it clashes with a built-in team), so use something like `cbadmin`.
2. Add a new **SQL Server** connection:
   - Host `db` (the compose service name, not `localhost`), port `1433`, database `Lounge`
   - User `sa`, password from `.env`
   - Enable **Trust server certificate** (or set driver property `trustServerCertificate=true`)

### Configuration
Everything is set in `.env` (git-ignored; `.env.example` is the template):

| Variable | Purpose |
|---|---|
| `SA_PASSWORD` | SQL Server `sa` password. 8+ chars with upper, lower, digit and symbol. Avoid `;`, `"` and `$`. |
| `ASPNETCORE_ENVIRONMENT` | `Development` locally, `Production` on a server |
| `COMPOSE_PROFILES` | `development` starts dev-only services (CloudBeaver). Leave empty in production. |
| `API_USERNAME`, `API_PASSWORD`, `API_PASSWORD_SECONDARY` | Basic-auth credentials for the write endpoints |

### Deploying to a server
The same compose file can run on a Linux server, but before exposing it:
- Set `ASPNETCORE_ENVIRONMENT=Production`, empty `COMPOSE_PROFILES`, and strong `SA_PASSWORD` / API credentials.
- Remove or bind to `127.0.0.1` the published `1433` and `10000` ports. Docker's port publishing bypasses `ufw`, so they'd otherwise be public.
- `MSSQL_PID: Developer` isn't licensed for production; use `Express` or a paid edition.
- The SQL Server image is x86-64 only.
- Put a reverse proxy (e.g. Caddy or nginx) in front for HTTPS, and set up database backups.

## Running without Docker
Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). `appsettings.Development.json` expects SQL Server LocalDB and the Azure Storage emulator (Windows), so on other platforms override the `DefaultConnection` and `AzureStorage` connection strings.

    dotnet tool install --global dotnet-ef        # once
    dotnet ef database update --project src/Lounge.Web
    dotnet run --project src/Lounge.Web

You can also debug from VS Code or Visual Studio.
