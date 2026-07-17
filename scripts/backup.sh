#!/bin/bash
# OreoLeads PostgreSQL Backup Script
# Usage: ./scripts/backup.sh [backup_dir]

set -euo pipefail

BACKUP_DIR="${1:-./backups}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/oreoleads_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "[$(date)] Starting backup..."

docker exec oreoleads-postgres pg_dump \
    -U "${POSTGRES_USER:-oreoleads}" \
    -d "${POSTGRES_DB:-oreoleads}" \
    --no-owner --no-acl \
    | gzip > "$BACKUP_FILE"

echo "[$(date)] Backup created: $BACKUP_FILE ($(du -h "$BACKUP_FILE" | cut -f1))"

# Cleanup old backups (keep last 30)
find "$BACKUP_DIR" -name "oreoleads_*.sql.gz" | sort | head -n -30 | xargs -r rm

echo "[$(date)] Backup complete."
