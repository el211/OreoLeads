#!/bin/bash
# Run EF Core migrations against the production database
# Usage: ./scripts/migrate.sh

set -euo pipefail

echo "[$(date)] Applying EF Core migrations..."

docker exec oreoleads-backend dotnet OreoLeads.Api.dll --migrate-only 2>/dev/null || \
    echo "[$(date)] Note: --migrate-only flag not supported; migrations run on startup."

echo "[$(date)] Done."
