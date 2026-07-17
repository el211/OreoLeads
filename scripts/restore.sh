#!/bin/bash
# OreoLeads PostgreSQL Restore Script
# Usage: ./scripts/restore.sh <backup_file>

set -euo pipefail

BACKUP_FILE="${1:-}"
if [[ -z "$BACKUP_FILE" || ! -f "$BACKUP_FILE" ]]; then
    echo "Usage: $0 <backup_file.sql.gz>"
    exit 1
fi

echo "[$(date)] WARNING: This will restore the database from $BACKUP_FILE"
echo "[$(date)] All current data will be replaced. Press Ctrl+C to cancel (5s)..."
sleep 5

echo "[$(date)] Restoring from $BACKUP_FILE..."

gunzip -c "$BACKUP_FILE" | docker exec -i oreoleads-postgres psql \
    -U "${POSTGRES_USER:-oreoleads}" \
    -d "${POSTGRES_DB:-oreoleads}"

echo "[$(date)] Restore complete."
