#!/usr/bin/env bash
set -euo pipefail

# Demo environment defaults (override via env variables)
DB_PORT="${DB_PORT:-14333}"
DB_PASSWORD="${DB_PASSWORD:-TestPassword123!}"
DB_CONTAINER="${DB_CONTAINER:-egitim_demo_mssql}"
API_PORT="${API_PORT:-5005}"
WEB_PORT="${WEB_PORT:-3000}"
DEMO_PASSWORD="${DEMO_PASSWORD:-Demo1234!}"

echo "=== [Egitim Platform Demo Startup] ==="

# 1. Start MSSQL container if not running
if docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
  if ! docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^${DB_CONTAINER}$"; then
    echo "Starting existing MSSQL container: ${DB_CONTAINER}..."
    docker start "${DB_CONTAINER}"
  else
    echo "MSSQL container is already running: ${DB_CONTAINER}"
  fi
else
  echo "Spawning fresh MSSQL container on port ${DB_PORT}..."
  docker run -d \
    --name "${DB_CONTAINER}" \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=${DB_PASSWORD}" \
    -p "${DB_PORT}:1433" \
    mcr.microsoft.com/mssql/server:2022-latest
fi

echo "Waiting for MSSQL port ${DB_PORT}..."
for i in {1..30}; do
  if nc -z localhost "${DB_PORT}" 2>/dev/null || (exec 3<>/dev/tcp/localhost/"${DB_PORT}") 2>/dev/null; then
    echo "MSSQL port is open."
    break
  fi
  sleep 1
done

echo ""
echo "=== Demo Environment Ready ==="
echo "To run the backend API:"
echo "  export ConnectionStrings__DefaultConnection=\"Server=localhost,${DB_PORT};Database=EgitimPlatform_Demo;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True;\""
echo "  export DemoSeed__Enabled=\"true\""
echo "  export DemoSeed__AdminPassword=\"${DEMO_PASSWORD}\""
echo "  export DemoSeed__TeacherPassword=\"${DEMO_PASSWORD}\""
echo "  export DemoSeed__CoachPassword=\"${DEMO_PASSWORD}\""
echo "  export DemoSeed__StudentPassword=\"${DEMO_PASSWORD}\""
echo "  export DemoSeed__ParentPassword=\"${DEMO_PASSWORD}\""
echo "  export CorsSettings__AllowedOrigins__0=\"http://localhost:${WEB_PORT}\""
echo "  export ASPNETCORE_URLS=\"http://localhost:${API_PORT}\""
echo "  dotnet run --project src/Api/EgitimPlatform.Api/EgitimPlatform.Api.csproj"
echo ""
echo "To run the frontend Web app:"
echo "  cd src/Web"
echo "  export VITE_API_BASE_URL=\"http://localhost:${API_PORT}\""
echo "  npm run dev -- --port ${WEB_PORT}"
echo ""
echo "Demo Accounts (Password: ${DEMO_PASSWORD}):"
echo "  - Admin:   admin.demo@egitim.com"
echo "  - Teacher: teacher.demo@egitim.com"
echo "  - Student: student.demo@egitim.com"
echo "  - Coach:   coach.demo@egitim.com"
echo "  - Parent:  parent.demo@egitim.com"
