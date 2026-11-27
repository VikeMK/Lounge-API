#!/usr/bin/env bash
# Production database backup: full SQL Server backup -> gzip -> upload to Cloudflare R2 -> prune old backups.
#
# Run on the server from cron, e.g. every 6 hours:
#   0 */6 * * * /path/to/Lounge-API/deploy/backup-db.sh >> /path/to/lounge-backup.log 2>&1
#
# Needs in .env: COMPOSE_FILE including compose.prod.yaml, R2_ENDPOINT, R2_ACCESS_KEY_ID, R2_SECRET_ACCESS_KEY
# (the token must be allowed on the backups bucket too), BACKUP_R2_BUCKET (optional: BACKUP_RETENTION, default 30d).
set -euo pipefail

# Run from the repo root so docker compose finds compose.yaml and .env
cd "$(dirname "$0")/.."

FILE="Lounge_$(date -u +%Y%m%d-%H%M%S).bak"
echo "$(date -u +%FT%TZ) starting backup $FILE"

# 1. The backup volume starts out owned by root; SQL Server runs as the "mssql" user and needs to write to it.
docker compose exec -T -u root db chown mssql /var/opt/mssql/backup

# 2. Full backup, written inside the db container to the shared backup volume.
#    \$MSSQL_SA_PASSWORD is expanded inside the container (it's already in db's environment).
#    CHECKSUM verifies pages while backing up. (Express edition has no backup COMPRESSION, hence gzip below.)
docker compose exec -T db sh -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -b \
  -Q \"BACKUP DATABASE [Lounge] TO DISK = N'/var/opt/mssql/backup/$FILE' WITH CHECKSUM, INIT\""

# 3. Compress, upload, delete the local copy, then delete remote backups older than the retention period.
docker compose run --rm -T -e FILE="$FILE" --entrypoint sh backup-upload -c '
  set -e
  : "${BACKUP_R2_BUCKET:?BACKUP_R2_BUCKET is not set in .env}"
  gzip "/backups/$FILE"
  rclone copy "/backups/$FILE.gz" "r2:$BACKUP_R2_BUCKET/lounge/"
  rm "/backups/$FILE.gz"
  rclone delete --min-age "$BACKUP_RETENTION" "r2:$BACKUP_R2_BUCKET/lounge/"
'

echo "$(date -u +%FT%TZ) uploaded $FILE.gz"
