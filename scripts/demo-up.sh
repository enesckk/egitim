#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

# Demo environment defaults (override via env variables)
DB_PORT="${DB_PORT:-14333}"
DB_PASSWORD="${DB_PASSWORD:-TestPassword123!}"
DB_CONTAINER="${DB_CONTAINER:-egitim_demo_mssql}"
API_PORT="${API_PORT:-5005}"
WEB_PORT="${WEB_PORT:-3000}"
DEMO_PASSWORD="${DEMO_PASSWORD:-Demo1234!}"

DOTNET_BIN="$(which dotnet 2>/dev/null || echo /usr/local/share/dotnet/dotnet)"
PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"
export PATH

echo "=== [1/4] Starting MSSQL Docker Container (${DB_CONTAINER}) ==="
if docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
  if ! docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
    echo "Starting existing MSSQL container..."
    docker start "${DB_CONTAINER}" >/dev/null
  else
    echo "MSSQL container is already running."
  fi
else
  echo "Spawning fresh MSSQL container on port ${DB_PORT}..."
  docker run -d \
    --name "${DB_CONTAINER}" \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=${DB_PASSWORD}" \
    -p "${DB_PORT}:1433" \
    mcr.microsoft.com/mssql/server:2022-latest >/dev/null
fi

echo "Waiting for MSSQL engine readiness on port ${DB_PORT}..."
MSSQL_READY=false
for i in {1..40}; do
  if docker logs "${DB_CONTAINER}" 2>&1 | grep -q "Recovery is complete"; then
    MSSQL_READY=true
    echo "MSSQL engine is fully recovered and ready."
    break
  fi
  sleep 1
done

if [ "$MSSQL_READY" = false ]; then
  echo "ERROR: MSSQL failed to initialize within 40 seconds."
  docker logs "${DB_CONTAINER}" | tail -n 20
  exit 1
fi

echo "=== [2/4] Starting Backend .NET 10 API (Port ${API_PORT}) ==="
# Free existing process on API_PORT if any
if lsof -ti :${API_PORT} >/dev/null 2>&1; then
  echo "Freeing existing process on port ${API_PORT}..."
  kill $(lsof -ti :${API_PORT}) 2>/dev/null || true
  sleep 1
fi

export DOTNET_CLI_HOME=/tmp/dotnet_home
export ConnectionStrings__DefaultConnection="Server=localhost,${DB_PORT};Database=EgitimPlatform_Demo;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True;"
export DemoSeed__Enabled="true"
export DemoSeed__AdminPassword="${DEMO_PASSWORD}"
export DemoSeed__TeacherPassword="${DEMO_PASSWORD}"
export DemoSeed__CoachPassword="${DEMO_PASSWORD}"
export DemoSeed__StudentPassword="${DEMO_PASSWORD}"
export DemoSeed__ParentPassword="${DEMO_PASSWORD}"
export CorsSettings__AllowedOrigins__0="http://localhost:${WEB_PORT}"
export RateLimiting__LoginLimit="100"
export RateLimiting__RefreshLimit="100"
export ASPNETCORE_URLS="http://localhost:${API_PORT}"
export ASPNETCORE_ENVIRONMENT="Development"

nohup "$DOTNET_BIN" run --project src/Api/EgitimPlatform.Api/EgitimPlatform.Api.csproj --no-build > /tmp/egitim_demo_api.log 2>&1 &
API_PID=$!
echo "$API_PID" > "$ROOT_DIR/.demo_api.pid"
echo "Backend API spawned (PID: ${API_PID}, Log: /tmp/egitim_demo_api.log)"

echo "Waiting for Backend API readiness on http://localhost:${API_PORT}..."
API_READY=false
for i in {1..35}; do
  if curl -s "http://localhost:${API_PORT}/health" >/dev/null 2>&1 || curl -s "http://localhost:${API_PORT}/api/v1/auth/login" >/dev/null 2>&1; then
    API_READY=true
    echo "Backend API is online and responding."
    break
  fi
  sleep 1
done

if [ "$API_READY" = false ]; then
  echo "ERROR: Backend API failed to become ready. Recent logs from /tmp/egitim_demo_api.log:"
  tail -n 25 /tmp/egitim_demo_api.log || true
  exit 1
fi

echo "=== [3/4] Starting Frontend Vite Dev Server (Port ${WEB_PORT}) ==="
if lsof -ti :${WEB_PORT} >/dev/null 2>&1; then
  echo "Freeing existing process on port ${WEB_PORT}..."
  kill $(lsof -ti :${WEB_PORT}) 2>/dev/null || true
  sleep 1
fi

export VITE_API_BASE_URL="http://localhost:${API_PORT}"
(cd src/Web && nohup npx vite --port "${WEB_PORT}" --no-open > /tmp/egitim_demo_web.log 2>&1 & echo $! > "$ROOT_DIR/.demo_web.pid")
WEB_PID=$(cat "$ROOT_DIR/.demo_web.pid")
echo "Frontend dev server spawned (PID: ${WEB_PID}, Log: /tmp/egitim_demo_web.log)"

echo "Waiting for Frontend readiness on http://localhost:${WEB_PORT}..."
WEB_READY=false
for i in {1..20}; do
  if curl -s "http://localhost:${WEB_PORT}" >/dev/null 2>&1; then
    WEB_READY=true
    echo "Frontend is online."
    break
  fi
  sleep 1
done

if [ "$WEB_READY" = false ]; then
  echo "ERROR: Frontend failed to start on port ${WEB_PORT}."
  tail -n 20 /tmp/egitim_demo_web.log || true
  exit 1
fi

echo ""
echo "=========================================================="
echo "    EGITIM PLATFORM DEMO STACK IS ONLINE & READY"
echo "=========================================================="
echo "  Web UI:      http://localhost:${WEB_PORT}"
echo "  Backend API: http://localhost:${API_PORT}"
echo "  Swagger UI:  http://localhost:${API_PORT}/swagger"
echo ""
echo "  Demo Accounts (Password: ${DEMO_PASSWORD}):"
echo "    - Admin:   admin.demo@egitim.com"
echo "    - Teacher: teacher.demo@egitim.com"
echo "    - Student: student.demo@egitim.com"
echo "    - Coach:   coach.demo@egitim.com"
echo "    - Parent:  parent.demo@egitim.com"
echo ""
echo "  To stop the demo stack at any time, run:"
echo "    ./scripts/demo-down.sh"
echo "=========================================================="
