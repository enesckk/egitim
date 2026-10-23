#!/usr/bin/env bash
set -euo pipefail

DB_CONTAINER="${DB_CONTAINER:-egitim_demo_mssql}"

echo "=== [Egitim Platform Demo Teardown] ==="
if docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
  echo "Stopping and removing ${DB_CONTAINER}..."
  docker stop "${DB_CONTAINER}" >/dev/null 2>&1 || true
  docker rm "${DB_CONTAINER}" >/dev/null 2>&1 || true
  echo "Container removed."
else
  echo "Container ${DB_CONTAINER} not found."
fi

echo "Demo environment cleaned up."
