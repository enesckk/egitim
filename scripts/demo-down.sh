#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

DB_CONTAINER="${DB_CONTAINER:-egitim_demo_mssql}"
API_PORT="${API_PORT:-5005}"
WEB_PORT="${WEB_PORT:-3000}"

echo "=== [Egitim Platform Demo Teardown] ==="

# 1. Stop Frontend Dev Server
if [ -f "$ROOT_DIR/.demo_web.pid" ]; then
  WEB_PID="$(cat "$ROOT_DIR/.demo_web.pid" 2>/dev/null || true)"
  if [ -n "$WEB_PID" ] && kill -0 "$WEB_PID" 2>/dev/null; then
    echo "Stopping Frontend server (PID: ${WEB_PID})..."
    kill "$WEB_PID" 2>/dev/null || true
  fi
  rm -f "$ROOT_DIR/.demo_web.pid"
fi
if lsof -ti :${WEB_PORT} >/dev/null 2>&1; then
  echo "Clearing lingering process on port ${WEB_PORT}..."
  kill $(lsof -ti :${WEB_PORT}) 2>/dev/null || true
fi

# 2. Stop Backend API
if [ -f "$ROOT_DIR/.demo_api.pid" ]; then
  API_PID="$(cat "$ROOT_DIR/.demo_api.pid" 2>/dev/null || true)"
  if [ -n "$API_PID" ] && kill -0 "$API_PID" 2>/dev/null; then
    echo "Stopping Backend API (PID: ${API_PID})..."
    kill "$API_PID" 2>/dev/null || true
  fi
  rm -f "$ROOT_DIR/.demo_api.pid"
fi
if lsof -ti :${API_PORT} >/dev/null 2>&1; then
  echo "Clearing lingering process on port ${API_PORT}..."
  kill $(lsof -ti :${API_PORT}) 2>/dev/null || true
fi

# 3. Stop MSSQL Container
if docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
  echo "Stopping and removing MSSQL container (${DB_CONTAINER})..."
  docker stop "${DB_CONTAINER}" >/dev/null 2>&1 || true
  docker rm "${DB_CONTAINER}" >/dev/null 2>&1 || true
fi

echo "Demo environment completely stopped and cleaned up."
