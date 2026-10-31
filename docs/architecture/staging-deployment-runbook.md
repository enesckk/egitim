# Bilim Akademi — Staging Deployment & Operations Runbook

This runbook defines the exact, executable procedures for deploying, validating, maintaining, and recovering the **Bilim Akademi** platform in an isolated staging environment on a fresh Ubuntu Linux server using the production Docker Compose stack (`docker-compose.prod.yml`).

---

## 1. Architecture Overview & Storage Topology

The platform executes as 5 containerized services connected via the isolated bridge network `egitim_internal`:

| Service | Image / Base | Role & Security Context | Exposed Ports | Persistent Volumes |
|---------|--------------|-------------------------|---------------|-------------------|
| `gateway` | `caddy:2-alpine` | HTTPS reverse proxy, automatic TLS (Let's Encrypt / ZeroSSL), HSTS & security headers | `80:80`, `443:443` (Host) | `caddy_data`, `caddy_config` |
| `web` | `nginx-unprivileged:alpine-slim` | Precompiled React/Vite SPA static asset server | `8080` (Internal only) | None |
| `api` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Backend REST API, non-root user (`$APP_UID`), output caching, rate limiting | `5005` (Internal only) | `api_storage` (`/app/Storage`) |
| `migrate` | `.NET 10 (Dockerfile.api)` | One-shot migration runner (`--initialize-platform`), applies EF migrations & roles | None | None |
| `mssql` | `mcr.microsoft.com/mssql/server:2022-latest` | Database engine, non-root user (`10001:10001`), internal TDS listener | `1433` (Internal only) | `mssql_data`, `mssql_backups` |

> [!CAUTION]
> **STRICT VOLUME PRESERVATION RULE:**
> Never execute `docker compose down -v`, `docker compose down --volumes`, `docker volume prune`, or `docker system prune --volumes` on staging or production hosts. Doing so will permanently destroy `mssql_data` (database files), `mssql_backups` (local backup archives), and `api_storage` (uploaded PDF materials).

---

## 2. Server Preparation & Host Hardening

### 2.1 Hardware Requirements
- **CPU:** Minimum 2 vCPUs (4 vCPUs recommended for MSSQL engine + .NET 10 + k6 concurrency).
- **RAM:** Minimum 4 GB RAM (8 GB recommended; SQL Server requires ≥ 2 GB dedicated memory).
- **Disk:** Minimum 40 GB NVMe/SSD on root partition `/`.

### 2.2 Operating System & Kernel Adjustments
On a fresh Ubuntu 22.04 or 24.04 LTS installation:

```bash
sudo apt-get update && sudo apt-get upgrade -y
sudo apt-get install -y curl git ufw ca-certificates gnupg lsb-release jq

# Ensure standard virtual memory settings for SQL Server Linux
sudo sysctl -w vm.max_map_count=262144
echo "vm.max_map_count=262144" | sudo tee -a /etc/sysctl.d/99-mssql.conf
```

### 2.3 Host Firewall (UFW)
Expose only SSH and the Caddy HTTPS gateway ports. Internal ports (`1433`, `5005`, `8080`) must remain blocked:

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp comment 'HTTP (Caddy ACME challenge & redirect)'
sudo ufw allow 443/tcp comment 'HTTPS (Caddy Gateway)'
sudo ufw --force enable
sudo ufw status verbose
```

### 2.4 Docker Engine & Compose Installation
Install official Docker Engine (v24+ with Compose v2):

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Verify Docker installation
docker --version
docker compose version
```

---

## 3. DNS, Secrets & Pre-Flight Configuration

### 3.1 DNS Routing
Create a DNS `A` record pointing the staging hostname to the server's public IPv4:
```text
staging.bilimakademi.com.    300    IN    A    <SERVER_PUBLIC_IP>
```
Verify propagation before starting Caddy so ACME certificate issuance succeeds:
```bash
dig +short staging.bilimakademi.com
```

### 3.2 Repository Deployment
Clone the repository and check out the verified baseline:
```bash
sudo mkdir -p /opt/egitim-platform
sudo chown -R $USER:$USER /opt/egitim-platform
git clone https://github.com/enesckk/egitim.git /opt/egitim-platform
cd /opt/egitim-platform
git checkout c6050a4
```

### 3.3 Secrets Configuration (`.env.production`)
Create a locked environment file:
```bash
cp .env.production.example .env.production
chmod 600 .env.production
```

Generate cryptographically secure secrets:
```bash
# Generate 48-byte base64 JWT signing key
JWT_KEY=$(openssl rand -base64 48)

# Generate 24-character alphanumeric database password
DB_PASS=$(openssl rand -base64 18 | tr -dc 'a-zA-Z0-9!#%' | head -c 20)
```

Edit `.env.production` with operator values:
```ini
# Domain & Gateway
SITE_ADDRESS="staging.bilimakademi.com"
CORS_ALLOWED_ORIGIN="https://staging.bilimakademi.com"
GATEWAY_HTTP_PORT=80
GATEWAY_HTTPS_PORT=443
VITE_API_BASE_URL="/"

# Database
MSSQL_SA_PASSWORD="<INSERT_DB_PASS>"
MSSQL_DATABASE="EgitimPlatform"
MSSQL_PID="Developer"
BACKUP_RETENTION_DAYS=14

# JWT Auth
JWT_SIGNING_KEY="<INSERT_JWT_KEY>"
JWT_ISSUER="EgitimPlatform"
JWT_AUDIENCE="EgitimPlatform"
JWT_ACCESS_TOKEN_MINUTES=15
JWT_REFRESH_TOKEN_DAYS=7

# Initial Bootstrap (Set to true only for first launch)
BOOTSTRAP_ENABLED=true
BOOTSTRAP_SUPERADMIN_EMAIL="superadmin@bilimakademi.com"
BOOTSTRAP_SUPERADMIN_PASSWORD="<STRONG_BOOTSTRAP_PASSWORD_MIN_12_CHARS>"

# Rate Limiting
RATE_LIMIT_LOGIN=10
RATE_LIMIT_REFRESH=30
RATE_LIMIT_UPLOAD=20
```

### 3.4 Configuration Validation Preflight
Validate Compose YAML interpolation and script syntax without launching containers:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.production config -q
bash -n scripts/mssql-backup.sh scripts/mssql-restore-verify.sh
```

---

## 4. Staging Stack Launch & Bootstrap Lockdown

### 4.1 Launch Stack & Apply Migrations
```bash
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
```

### 4.2 Monitor One-Shot Migration Runner
Follow the `migrate` container logs until completion:
```bash
docker compose -f docker-compose.prod.yml logs -f migrate
```
Expected output:
```text
[INFO] Running platform initialization...
[INFO] Database migrations applied.
[INFO] System roles seeded.
[INFO] Bootstrap SuperAdmin processed.
[INFO] Platform initialization complete. Exiting.
```
Confirm `migrate` exited with code `0`:
```bash
docker compose -f docker-compose.prod.yml ps -a migrate
```

### 4.3 Immediate Post-Bootstrap Lockdown
Once the SuperAdmin account is created, disable bootstrap credentials immediately to prevent unintended password resets on container restarts:
```bash
sed -i 's/^BOOTSTRAP_ENABLED=true/BOOTSTRAP_ENABLED=false/' .env.production
sed -i 's/^BOOTSTRAP_SUPERADMIN_PASSWORD=.*/BOOTSTRAP_SUPERADMIN_PASSWORD=""/' .env.production
```

### 4.4 Health & Container Readiness Verification
Ensure all 4 long-running services report `healthy`:
```bash
docker compose -f docker-compose.prod.yml ps
```
Verify internal readiness and liveness endpoints through Caddy:
```bash
# Gateway Liveness (process up)
curl -fsS https://staging.bilimakademi.com/health/live

# Backend & Database Readiness (verifies MSSQL connectivity)
curl -fsS https://staging.bilimakademi.com/health/ready
```
Expected JSON response:
```json
{"status":"Healthy"}
```

---

## 5. Five-Portal Staging Acceptance Verification

Log in through `https://staging.bilimakademi.com/login` and verify all five roles:

1. **Admin Portal (`/admin`):**
   - Log in as `superadmin@bilimakademi.com`.
   - Verify Overview dashboard renders institution metrics without errors.
   - Navigate to `/admin/teachers` -> Click "Yeni Öğretmen Ekle" -> Create a teacher (verifies `institutionId` GUID resolution, returns 201).
   - Navigate to `/admin/coaches` -> Click "Yeni Koç Ekle" -> Create a coach (returns 201).
   - Navigate to `/admin/students` -> Create a student and assign the coach.

2. **Teacher Portal (`/teacher`):**
   - Log in with created teacher credentials.
   - View assigned classroom list and student directory.
   - Click "Mesaj Gönder" on a student card -> Verify deep link navigates to `/teacher/messages?studentId=...&userId=...` with the student preselected. Send a message.

3. **Student Portal (`/student`):**
   - Log in with created student credentials.
   - Verify Today dashboard, Academic Intelligence, and Study Plans.
   - Navigate to `/student/messages` -> Confirm message from teacher is received and reply.
   - Verify layout responsiveness at 390px, 768px, and 1440px.

4. **Coach Portal (`/coach`):**
   - Log in with coach credentials.
   - View assigned student list -> Open student detail.
   - Create a Coach Task -> Confirm student sees it in Study Plans.
   - Create a Private Coach Note -> Confirm note is saved and strictly isolated from student/parent view.

5. **Parent Portal (`/parent`):**
   - Link a parent to the student via Admin or seed data.
   - Log in as parent -> Verify Student Summary shows only the linked child.
   - Confirm unlinked student access (`/api/v1/students/{unlinkedId}/360`) returns `403 Forbidden` or `404 Not Found`.

6. **Persistent Upload Storage Verification:**
   - In Teacher portal, upload a course material (PDF ≤ 20 MB).
   - Confirm upload succeeds with streaming SHA-256 hash.
   - Log in as student, download the uploaded material, and verify file integrity.
   - Verify container storage on host:
     ```bash
     docker compose -f docker-compose.prod.yml exec api ls -la /app/Storage/Materials
     ```

---

## 6. Backup Operations & Disaster Recovery

### 6.1 Native Database Backup
Execute native SQL Server backup with CHECKSUM and COMPRESSION:
```bash
./scripts/mssql-backup.sh --env-file .env.production
```
The script outputs:
- Verified `.bak` file in `mssql_backups` volume (`/var/opt/mssql-backups/`).
- SHA-256 checksum manifest (`<backup>.bak.sha256`).
- Automatic validation via `RESTORE VERIFYONLY WITH CHECKSUM`.

### 6.2 Uploaded Files Backup (`api_storage`)
Archive the `/app/Storage` persistent volume:
```bash
BACKUP_DATE=$(date -u +'%Y%m%d_%H%M%SZ')
mkdir -p /opt/backups/materials
docker run --rm \
  -v egitim-platform_api_storage:/data:ro \
  -v /opt/backups/materials:/backup \
  alpine tar czf "/backup/materials_${BACKUP_DATE}.tar.gz" -C /data .
```

### 6.3 Off-Host Encrypted Replication (Operational Requirement)
Local backups must be encrypted and pushed to off-host cloud object storage (e.g., S3, B2, or Azure Blob) using GPG or SSE-KMS:
```bash
# Example using gpg symmetric encryption
gpg --symmetric --cipher-algo AES256 --batch --passphrase "$BACKUP_ENCRYPTION_KEY" \
  /opt/backups/materials/materials_${BACKUP_DATE}.tar.gz
```

### 6.4 Host Scheduling (Cron / Systemd Timer)
Schedule daily backups at 02:00 UTC on the host:
```bash
# /etc/cron.d/egitim-platform-backup
0 2 * * * root cd /opt/egitim-platform && ./scripts/mssql-backup.sh --env-file .env.production >> /var/log/egitim-backup.log 2>&1
30 2 * * * root /opt/egitim-platform/scripts/backup-materials.sh >> /var/log/egitim-backup.log 2>&1
```

### 6.5 Isolated Restore Drill
Periodically test backup restoration without touching live data:
```bash
./scripts/mssql-restore-verify.sh --env-file .env.production --latest
```

---

## 7. Application Rollback & Migration Compatibility

### 7.1 Safe Application Rollback
To roll back a code release without affecting persistent data:
```bash
cd /opt/egitim-platform

# 1. Take a precautionary full database backup
./scripts/mssql-backup.sh --env-file .env.production

# 2. Stop running application containers
docker compose -f docker-compose.prod.yml down

# 3. Check out target previous commit
git checkout <PREVIOUS_COMMIT_SHA>

# 4. Rebuild and launch the previous application version
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build

# 5. Verify health
curl -fsS https://staging.bilimakademi.com/health/ready
```

### 7.2 Database Migration Compatibility Limitations
- **Additive Migrations (Expand Phase):** Safe to roll back application code. New nullable columns or new tables will simply be ignored by older code.
- **Breaking / Destructive Migrations (Contract Phase):** If a migration renamed tables, dropped columns, or altered data types, older application code will fail.
  - In such cases, rolling back application code **requires** restoring the pre-deployment database backup using `scripts/mssql-restore-verify.sh`.
  - Restoring a database backup resets data to the backup snapshot timestamp; any user transactions created between deployment and rollback will be lost (RPO = time since pre-deployment backup).
  - Rule: Database schema changes must follow the Expand-Contract pattern across releases to avoid forced database rollbacks.
