#!/usr/bin/env bash
# ==============================================================================
# Egitim Platformu — Production MSSQL Backup & Verification Script
#
# Performs a native SQL Server Full Database Backup with CHECKSUM and automatic
# COMPRESSION (where supported by the SQL Server edition), stores the backup on
# a dedicated persistent backup mount (/var/opt/mssql-backups), generates a
# SHA-256 integrity file, and runs RESTORE VERIFYONLY WITH CHECKSUM.
#
# Security & Safety Guarantees:
#   - Never exposes credentials in process arguments, logs, or stdout (set +x).
#   - Rejects backup paths inside the database data volume (/var/opt/mssql).
#   - Verifies that the backup directory is a dedicated mounted volume.
#   - Prunes expired backups ONLY after a new backup passes RESTORE VERIFYONLY.
# ==============================================================================

set -euo pipefail
set +x

SCRIPT_NAME="$(basename "$0")"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-}"
CONTAINER_NAME="${MSSQL_CONTAINER:-}"
DB_NAME="${MSSQL_DATABASE:-}"
BACKUP_DIR="${MSSQL_BACKUP_DIR:-/var/opt/mssql-backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"
COPY_ONLY=0
VERIFY_ONLY_FILE=""

usage() {
  cat <<EOF
Usage: $SCRIPT_NAME [OPTIONS]

Options:
  --container <name>        Target MSSQL container name (auto-detected from Compose if omitted)
  --compose-file <path>     Compose file for container discovery (default: docker-compose.prod.yml)
  --env-file <path>         Optional .env file to load MSSQL_DATABASE / BACKUP_RETENTION_DAYS
  --database <name>         Database name to back up (default: EgitimPlatform)
  --backup-dir <path>       Dedicated backup mount path in container (default: /var/opt/mssql-backups)
  --retention-days <days>   Delete backups older than N days after successful verify (default: 14, 0=disable)
  --copy-only               Perform a COPY_ONLY full backup (preserves differential backup chain)
  --verify-only <file.bak>  Verify an existing backup file (SHA-256 + RESTORE VERIFYONLY WITH CHECKSUM)
  -h, --help                Show this help message
EOF
}

log_info() {
  printf '[%s] [INFO] %s\n' "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" "$*"
}

log_error() {
  printf '[%s] [FAIL] %s\n' "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" "$*" >&2
}

log_pass() {
  printf '[%s] [PASS] %s\n' "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" "$*"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --container)
      CONTAINER_NAME="${2:-}"
      shift 2
      ;;
    --compose-file)
      COMPOSE_FILE="${2:-}"
      shift 2
      ;;
    --env-file)
      ENV_FILE="${2:-}"
      shift 2
      ;;
    --database)
      DB_NAME="${2:-}"
      shift 2
      ;;
    --backup-dir)
      BACKUP_DIR="${2:-}"
      shift 2
      ;;
    --retention-days)
      RETENTION_DAYS="${2:-}"
      shift 2
      ;;
    --copy-only)
      COPY_ONLY=1
      shift
      ;;
    --verify-only)
      VERIFY_ONLY_FILE="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      log_error "Unknown argument: $1"
      usage >&2
      exit 2
      ;;
  esac
done

# Load non-secret defaults from ENV_FILE if provided
if [[ -n "$ENV_FILE" ]]; then
  if [[ ! -f "$ENV_FILE" ]]; then
    log_error "Specified --env-file does not exist: $ENV_FILE"
    exit 2
  fi
  if [[ -z "$DB_NAME" ]]; then
    DB_NAME="$(grep -E '^MSSQL_DATABASE=' "$ENV_FILE" | tail -n 1 | cut -d= -f2- | tr -d '"'\''' || true)"
  fi
fi

DB_NAME="${DB_NAME:-EgitimPlatform}"

# Validate database identifier (alphanumeric, underscore, hyphen only)
if [[ ! "$DB_NAME" =~ ^[A-Za-z0-9_-]+$ ]]; then
  log_error "Invalid database name '$DB_NAME'. Only alphanumeric, underscore, and hyphen characters are allowed."
  exit 2
fi

# Validate retention days
if [[ ! "$RETENTION_DAYS" =~ ^[0-9]+$ ]]; then
  log_error "Invalid --retention-days value '$RETENTION_DAYS'. Must be a non-negative integer."
  exit 2
fi

# Normalize and validate backup directory: must NOT be inside /var/opt/mssql data volume
BACKUP_DIR="${BACKUP_DIR%/}"
if [[ -z "$BACKUP_DIR" || "$BACKUP_DIR" == "/var/opt/mssql" || "$BACKUP_DIR" == /var/opt/mssql/* ]]; then
  log_error "Backup directory '$BACKUP_DIR' is invalid. Backups must be stored outside the database data volume (/var/opt/mssql)."
  exit 2
fi

# Discover container if not explicitly provided
if [[ -z "$CONTAINER_NAME" ]]; then
  if [[ -n "$ENV_FILE" && -f "$COMPOSE_FILE" ]]; then
    CONTAINER_NAME="$(docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps -q mssql 2>/dev/null | head -n 1 || true)"
  elif [[ -f "$COMPOSE_FILE" ]]; then
    CONTAINER_NAME="$(docker compose -f "$COMPOSE_FILE" ps -q mssql 2>/dev/null | head -n 1 || true)"
  fi
fi

if [[ -z "$CONTAINER_NAME" ]]; then
  log_error "Could not determine target MSSQL container. Specify --container <name> or ensure Compose stack is running."
  exit 2
fi

# Verify container is running
if [[ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER_NAME" 2>/dev/null || echo false)" != "true" ]]; then
  log_error "Target MSSQL container '$CONTAINER_NAME' is not running."
  exit 1
fi

# Verify that BACKUP_DIR is a mounted volume inside the container and enforce 10001:10001 (mssql) permissions
docker exec -u 0 "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  BACKUP_DIR="$1"
  if ! grep -qs " ${BACKUP_DIR} " /proc/mounts; then
    echo "ERROR: ${BACKUP_DIR} is not mounted as a dedicated persistent volume in container." >&2
    exit 3
  fi
  mkdir -p "${BACKUP_DIR}"
  chown 10001:10001 "${BACKUP_DIR}"
  chmod 0750 "${BACKUP_DIR}"
' -- "$BACKUP_DIR" || {
  log_error "Backup mount validation failed for '$BACKUP_DIR' in container '$CONTAINER_NAME'."
  exit 1
}

verify_backup_artifact() {
  local target_bak="$1"
  if [[ "$target_bak" != /* ]]; then
    target_bak="${BACKUP_DIR}/${target_bak}"
  fi

  log_info "Verifying backup artifact: ${target_bak}"

  # 1. File existence, non-zero size, and SHA-256 check (if sidecar exists)
  docker exec -u 0 "$CONTAINER_NAME" bash -c '
    set -euo pipefail
    BAK="$1"
    if [[ ! -s "$BAK" ]]; then
      echo "ERROR: Backup file does not exist or is empty: $BAK" >&2
      exit 1
    fi
    if [[ -f "${BAK}.sha256" ]]; then
      cd "$(dirname "$BAK")"
      sha256sum -c "$(basename "${BAK}.sha256")" >/dev/null
    fi
  ' -- "$target_bak" || {
    log_error "Filesystem or SHA-256 integrity check failed for '${target_bak}'."
    return 1
  }

  # 2. SQL Server RESTORE VERIFYONLY WITH CHECKSUM
  docker exec -i "$CONTAINER_NAME" bash -c '
    set -euo pipefail
    set +x
    export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
    if [[ -z "$SQLCMDPASSWORD" ]]; then
      echo "ERROR: Database credentials are not configured in container environment." >&2
      exit 1
    fi
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -v BACKUP_PATH="$1" -i /dev/stdin
  ' -- "$target_bak" <<'EOSQL'
SET NOCOUNT ON;
DECLARE @BackupPath nvarchar(1000) = N'$(BACKUP_PATH)';
DECLARE @VerifySql nvarchar(max) = N'RESTORE VERIFYONLY FROM DISK = N'
    + QUOTENAME(@BackupPath, '''')
    + N' WITH CHECKSUM, STOP_ON_ERROR;';
EXEC sp_executesql @VerifySql;
EOSQL

  log_pass "Backup verification succeeded (RESTORE VERIFYONLY WITH CHECKSUM): ${target_bak}"
}

# Handle --verify-only mode
if [[ -n "$VERIFY_ONLY_FILE" ]]; then
  verify_backup_artifact "$VERIFY_ONLY_FILE"
  exit 0
fi

TIMESTAMP="$(date -u +'%Y%m%dT%H%M%SZ')"
BACKUP_FILENAME="${DB_NAME}_${TIMESTAMP}.bak"
BACKUP_FULL_PATH="${BACKUP_DIR}/${BACKUP_FILENAME}"

log_info "Starting full backup of database '${DB_NAME}' to '${BACKUP_FULL_PATH}'..."

# Execute native BACKUP DATABASE with CHECKSUM and automatic COMPRESSION detection
docker exec -i "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  set +x
  export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
  if [[ -z "$SQLCMDPASSWORD" ]]; then
    echo "ERROR: Database credentials are not configured in container environment." >&2
    exit 1
  fi
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b \
    -v DB_NAME="$1" BACKUP_PATH="$2" COPY_ONLY="$3" \
    -i /dev/stdin
' -- "$DB_NAME" "$BACKUP_FULL_PATH" "$COPY_ONLY" <<'EOSQL'
SET NOCOUNT ON;

DECLARE @DbName sysname = N'$(DB_NAME)';
DECLARE @BackupPath nvarchar(1000) = N'$(BACKUP_PATH)';
DECLARE @UseCopyOnly bit = $(COPY_ONLY);

IF DB_ID(@DbName) IS NULL
BEGIN
    RAISERROR(N'Target database [%s] does not exist.', 16, 1, @DbName);
    RETURN;
END

DECLARE @SupportsCompression bit = CASE
    WHEN SERVERPROPERTY('EngineEdition') IN (2, 3)
         AND EXISTS (SELECT 1 FROM sys.configurations WHERE name = N'backup compression default')
    THEN 1
    ELSE 0
END;

DECLARE @Options nvarchar(400) = N'FORMAT, INIT, CHECKSUM, STOP_ON_ERROR, STATS = 25';
IF @SupportsCompression = 1
    SET @Options = @Options + N', COMPRESSION';
IF @UseCopyOnly = 1
    SET @Options = @Options + N', COPY_ONLY';

DECLARE @BackupName nvarchar(256) = @DbName + N' - Full Database Backup';
DECLARE @Sql nvarchar(max) = N'BACKUP DATABASE ' + QUOTENAME(@DbName)
    + N' TO DISK = N' + QUOTENAME(@BackupPath, '''')
    + N' WITH ' + @Options
    + N', NAME = N' + QUOTENAME(@BackupName, '''') + N';';

PRINT N'Executing BACKUP DATABASE with options: ' + @Options;
EXEC sp_executesql @Sql;

SELECT TOP (1)
    database_name AS [DatabaseName],
    recovery_model AS [RecoveryModel],
    CAST(backup_size / 1024.0 / 1024.0 AS decimal(12, 2)) AS [BackupSizeMB],
    CAST(compressed_backup_size / 1024.0 / 1024.0 AS decimal(12, 2)) AS [CompressedSizeMB],
    has_backup_checksums AS [HasChecksums],
    is_copy_only AS [IsCopyOnly]
FROM msdb.dbo.backupset
WHERE database_name = @DbName AND type = 'D'
ORDER BY backup_finish_date DESC;
EOSQL

# Generate SHA-256 sidecar and enforce restrictive file permissions (0640, owned by mssql)
docker exec -u 0 "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  BACKUP_DIR="$1"
  BACKUP_FILENAME="$2"
  cd "${BACKUP_DIR}"
  if [[ ! -s "${BACKUP_FILENAME}" ]]; then
    echo "ERROR: Created backup file is missing or empty: ${BACKUP_FILENAME}" >&2
    exit 1
  fi
  sha256sum "${BACKUP_FILENAME}" > "${BACKUP_FILENAME}.sha256"
  chown 10001:10001 "${BACKUP_FILENAME}" "${BACKUP_FILENAME}.sha256"
  chmod 0640 "${BACKUP_FILENAME}" "${BACKUP_FILENAME}.sha256"
' -- "$BACKUP_DIR" "$BACKUP_FILENAME"

# Run mandatory post-backup verification (SHA-256 + RESTORE VERIFYONLY WITH CHECKSUM)
verify_backup_artifact "$BACKUP_FULL_PATH"

# Apply local retention policy ONLY after new backup and verification have succeeded
if [[ "$RETENTION_DAYS" -gt 0 ]]; then
  PRUNED_COUNT="$(docker exec -u 0 "$CONTAINER_NAME" bash -c '
    set -euo pipefail
    BACKUP_DIR="$1"
    DB_NAME="$2"
    RETENTION_DAYS="$3"
    CURRENT_BAK="$4"
    count=0
    while IFS= read -r -d "" file; do
      base="$(basename "$file")"
      if [[ "$base" == "$CURRENT_BAK" || "$base" == "${CURRENT_BAK}.sha256" ]]; then
        continue
      fi
      rm -f "$file"
      count=$((count + 1))
    done < <(find "$BACKUP_DIR" -maxdepth 1 -type f \( -name "${DB_NAME}_*.bak" -o -name "${DB_NAME}_*.bak.sha256" \) -mtime +"$RETENTION_DAYS" -print0)
    echo "$count"
  ' -- "$BACKUP_DIR" "$DB_NAME" "$RETENTION_DAYS" "$BACKUP_FILENAME")"
  log_info "Retention check complete (policy: ${RETENTION_DAYS} days, pruned artifacts: ${PRUNED_COUNT})."
fi

log_pass "Backup completed and verified: ${BACKUP_FULL_PATH}"
