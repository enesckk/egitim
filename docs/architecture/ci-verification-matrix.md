# CI vs. Staging / Production Verification Matrix

This document defines the boundary between automated **GitHub Actions CI checks** ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)) and **Staging / Pre-Release Verification checks** that require a stateful SQL Server 2022 instance or full multi-container runtime.

---

## 1. Automated GitHub Actions CI Checks (`push` & `pull_request` to `main`)

The CI workflow runs three parallel, stateless jobs on `ubuntu-latest` with least-privilege permissions (`contents: read`), concurrency cancellation for superseded runs, and zero external secrets required:

| CI Job | Validated Scope | Commands Executed |
|--------|-----------------|-------------------|
| **`backend`** | .NET 10 restore, Release compilation across all 15 modules + API, and fast in-memory Unit Tests (validators, domain rules, reporting adherence math, health checks, demo seeder logic). | `dotnet restore tests/EgitimPlatform.Unit/EgitimPlatform.Unit.csproj`<br>`dotnet build ... --configuration Release --no-restore`<br>`dotnet test ... --configuration Release --no-build` |
| **`frontend`** | Node 22 clean dependency install (`npm ci`), strict TypeScript project reference checks (`tsc -b`), Vite production bundle compilation, and fail-closed production environment & JWT session lifecycle unit tests. | `npm ci`<br>`npm run typecheck`<br>`VITE_API_BASE_URL=/ npx vite build`<br>`npx playwright test e2e/auth-unit.spec.ts e2e/prod-env.spec.ts` |
| **`docker`** | Production `docker-compose.prod.yml` schema and variable interpolation validation (using safe non-secret placeholders without starting containers), shell syntax validation of operational scripts, and multi-stage builds of `Dockerfile.api` and `Dockerfile.web`. | `docker compose -f docker-compose.prod.yml config -q`<br>`bash -n scripts/*.sh`<br>`docker buildx build -f Dockerfile.api ...`<br>`docker buildx build -f Dockerfile.web ...` |

### Security Guarantees in CI
- **No Secrets Required or Printed**: Ordinary CI validation requires zero GitHub Actions secrets. Placeholder values used for `docker compose config -q` are non-sensitive syntax tokens and never connect to any infrastructure.
- **No Automatic Deployment or Migrations**: CI never runs `--initialize-platform`, database migrations, or container deployments against external environments.

---

## 2. Checks Excluded from Default PR CI (Require Disposable MSSQL / Staging Runtime)

The following test suites require a live SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) instance, full container networking, or persistent volumes, and are intentionally separated from the fast default PR workflow:

| Verification Suite | Why Excluded from Stateless PR CI | How to Execute (Local / Staging Pre-Release) |
|--------------------|-----------------------------------|----------------------------------------------|
| **Backend Integration Tests**<br>(`tests/EgitimPlatform.Integration`) | Requires a live MSSQL 2022 instance to verify EF Core migrations, SQL constraints, transactions, and query translation. | Start disposable MSSQL container, then run:<br>`dotnet test tests/EgitimPlatform.Integration/EgitimPlatform.Integration.csproj` |
| **Backend Security & Tenant Isolation Tests**<br>(`tests/EgitimPlatform.Security`) | Requires a live MSSQL 2022 instance to verify `GlobalQueryFilter` tenant isolation, RBAC policies, CSRF origin enforcement, and cross-institution IDOR prevention against real database state. | Start disposable MSSQL container, then run:<br>`dotnet test tests/EgitimPlatform.Security/EgitimPlatform.Security.csproj` |
| **Fullstack Playwright E2E Suite**<br>(`src/Web/playwright.fullstack.config.ts`) | Requires running MSSQL + seeded `.NET 10` API (`:5005`) + Vite/Web frontend (`:3000`) + Chromium browser binaries. | `./scripts/demo-up.sh`<br>`cd src/Web && npm run test:e2e:fullstack` |
| **Production Compose & Caddy HTTPS Smoke Test**<br>(`docker-compose.prod.yml` + `Caddyfile`) | Requires starting the 5-service stack (`mssql`, `migrate`, `api`, `web`, `gateway`), executing `--initialize-platform`, and probing TLS endpoints. | `docker compose -p egitim_verify -f docker-compose.prod.yml --env-file .env.production up -d --wait`<br>`curl -k https://localhost/health/ready`<br>`docker compose -p egitim_verify ... down -v` |
| **MSSQL Backup & Isolated Restore Verification**<br>(`scripts/mssql-backup.sh` & `scripts/mssql-restore-verify.sh`) | Requires running `mssql` container with mounted `mssql_data` and `mssql_backups` volumes to test `BACKUP DATABASE ... WITH CHECKSUM, COMPRESSION`, `RESTORE VERIFYONLY`, and `DBCC CHECKDB`. | `./scripts/mssql-backup.sh --env-file .env.production`<br>`./scripts/mssql-restore-verify.sh --env-file .env.production --latest` |
