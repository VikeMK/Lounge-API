# Lounge-API

## Running locally with Docker (recommended)

Runs the whole stack in containers: the API, SQL Server, S3Mock (a local S3 emulator standing in for Cloudflare R2, where table images are stored) and a web database UI. You don't need .NET or SQL Server installed.

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
| http://localhost:9090/lounge-table-images/&lt;id&gt;.png | Stored table images in S3Mock (Development only) |
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
| `COMPOSE_PROFILES` | `development` starts dev-only services (CloudBeaver, S3Mock). Leave empty in production. |
| `API_USERNAME`, `API_PASSWORD`, `API_PASSWORD_SECONDARY` | Basic-auth credentials for the write endpoints |
| `R2_ENDPOINT`, `R2_ACCESS_KEY_ID`, `R2_SECRET_ACCESS_KEY`, `R2_BUCKET` | Table image bucket (S3 API). Unset locally, where S3Mock is used; required in production. |
| `TABLE_IMAGE_BASE_URL` | Public URL of the bucket that `/TableImage` redirects to (e.g. `https://images.example.com`) |
| `CLOUDFLARE_ZONE_ID`, `CLOUDFLARE_PURGE_TOKEN` | Purges re-uploaded table images from Cloudflare's cache. Unset locally (purging is skipped); required in production. |
| `COMPOSE_FILE` | Production only: `compose.yaml:compose.prod.yaml` |
| `CLOUDFLARE_TUNNEL_TOKEN` | Production only: Cloudflare Tunnel token for `cloudflared` |
| `MSSQL_PID` | Production only: SQL Server edition (default `Express`, max 10 GB per database) |
| `BACKUP_R2_BUCKET`, `BACKUP_RETENTION` | Production only: R2 bucket for database backups, and how long to keep them (default `30d`) |

### How table images work
Table images are drawn by an external service (`gb.hlorenzi.com`) and stored in an S3-compatible bucket: Cloudflare R2 in production, S3Mock locally. The app never downloads images from the bucket, so scraping images doesn't load the server.

- **`GET /TableImage/{id}.png`** returns a **302 redirect** to `{TABLE_IMAGE_BASE_URL}/{id}.png`, cacheable by Cloudflare for 5 minutes.
- The **`TableImages`** database table records which images exist. It is deliberately separate from `Tables` and not change-tracked, so writing to it never triggers the stats cache to recompute.
- If there is no row yet, the app checks the bucket (a `HEAD` request, no download). If the image is there it records a row and redirects; if not, it generates the image, uploads it, records a row and returns the PNG directly.
- In production, a Cloudflare **cache rule** keeps images at the edge for a long time and **ignores query strings**, so adding `?anything` to an image URL can't bypass the cache and cause R2 reads. Browsers cache images for 5 minutes.
- Whenever the app uploads an image (table created, scores changed, or generated on request), it **purges that URL** from Cloudflare's cache, so the next request gets the new image.

## Deploying to a server
Production runs the same stack with [`compose.prod.yaml`](compose.prod.yaml) layered on top: no published ports, SQL Server Express, a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/) (`cloudflared`) as the only way in, table images in Cloudflare R2, and database backups to R2. Examples below use `lounge.example.com` for the site and `images.example.com` for table images.

### Server
- **x86-64** Linux (the SQL Server image doesn't run on ARM), around **16 GB RAM**. Firewall: SSH only; the tunnel is outbound-only.
- Docker Engine with **Compose v2.24+** (`compose.prod.yaml` uses `!reset`), installed from [Docker's apt repository](https://docs.docker.com/engine/install/ubuntu/).
- Limit container log sizes, or they grow until the disk is full:

      echo '{ "log-driver": "local", "log-opts": { "max-size": "20m", "max-file": "5" } }' | sudo tee /etc/docker/daemon.json
      sudo systemctl restart docker

- SQL Server **Express** is free but limited to 10 GB per database; set `MSSQL_PID` to another edition if you need more and are licensed for it.

### Cloudflare
1. **Tunnel**: Zero Trust → Networks → Tunnels → create a *Cloudflared* tunnel, copy its token, and add a public hostname `lounge.example.com` → `http://web:8080`.
2. **R2 buckets**:
   - `lounge-table-images`: connect the custom domain `images.example.com`; leave the `r2.dev` URL disabled.
   - `lounge-db-backups`: private (no domain). Optionally add a bucket lock rule (prefix `lounge/`, 30 days) so backups can't be deleted early.
3. **R2 API token**: Object Read & Write, limited to both buckets. Note the access key ID, secret and S3 endpoint.
4. **Cache rule** for hostname `images.example.com`: eligible for cache, edge TTL 1 year (ignore origin), browser TTL as short as allowed (ideally 5 minutes), cache key **ignores query strings**.
5. **Purge token**: My Profile → API Tokens → custom token with *Zone → Cache Purge → Purge* on your zone. Note the zone ID (zone overview page).

### Configure and start
    git clone <repo-url> && cd Lounge-API
    cp .env.example .env && chmod 600 .env

In `.env`: strong `SA_PASSWORD` and API credentials, `ASPNETCORE_ENVIRONMENT=Production`, empty `COMPOSE_PROFILES`, then uncomment and fill in the production section (`COMPOSE_FILE`, `CLOUDFLARE_TUNNEL_TOKEN`, `R2_*`, `TABLE_IMAGE_BASE_URL=https://images.example.com`, `CLOUDFLARE_ZONE_ID`, `CLOUDFLARE_PURGE_TOKEN`, `BACKUP_R2_BUCKET`).

    docker compose config --services   # lists cloudflared (not dbui/s3mock); fails if a required variable is missing
    docker compose up -d --build
    docker compose logs cloudflared | grep -i registered

The tunnel shows as *Healthy* in Zero Trust once connected. To check images: `curl -sI "https://images.example.com/<id>.png?v=a"` twice with different `?v=` values should both show `cf-cache-status: HIT`.

### Backups
[`deploy/backup-db.sh`](deploy/backup-db.sh) takes a full backup, gzips it, uploads it to `BACKUP_R2_BUCKET` under `lounge/` and deletes backups older than `BACKUP_RETENTION` (default `30d`). Run it once by hand, then from cron (SQL Server Express has no scheduler):

    0 */6 * * * /home/<user>/Lounge-API/deploy/backup-db.sh >> /home/<user>/lounge-backup.log 2>&1

To restore (replaces the current database):

    docker compose run --rm backup-upload copy r2:lounge-db-backups/lounge/Lounge_<timestamp>.bak.gz /backups/
    docker compose run --rm --entrypoint gunzip backup-upload /backups/Lounge_<timestamp>.bak.gz
    docker compose stop web
    docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<SA_PASSWORD>' -C \
      -Q "RESTORE DATABASE [Lounge] FROM DISK = N'/var/opt/mssql/backup/Lounge_<timestamp>.bak' WITH REPLACE"
    docker compose start web

### Updating
    git pull && docker compose up -d --build

`migrate` applies any new migrations before `web` restarts.

## Running without Docker
Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). `appsettings.Development.json` expects SQL Server LocalDB (Windows; on other platforms override the `DefaultConnection` connection string) and S3Mock on `localhost:9090` for table images, e.g. `docker compose up -d s3mock`.

    dotnet tool install --global dotnet-ef        # once
    dotnet ef database update --project src/Lounge.Web
    dotnet run --project src/Lounge.Web

You can also debug from VS Code or Visual Studio.
