#!/usr/bin/env bash
# ==============================================================================
# Egitim Platformu — Safe MSSQL Restore & Validation Script
#
# Validates a SQL Server .bak archive by:
#   1. Verifying filesystem presence and SHA-256 sidecar checksum (if present).
#   2. Executing RESTORE VERIFYONLY FROM DISK WITH CHECKSUM, STOP_ON_ERROR.
#   3. Restoring the backup into an ISOLATED DISPOSABLE database (using MOVE
#      to isolated physical .mdf/.ldf paths) — NEVER overwriting an existing DB.
#   4. Running DBCC CHECKDB (PHYSICAL_ONLY) and verifying EF Core migrations
#      and core domain tables on the restored database.
#   5. Dropping the temporary validation database automatically (unless
#      --keep-restored-db is explicitly passed for new-database recovery).
#
# Security & Safety Guarantees:
#   - Never exposes credentials in process arguments, logs, or stdout (set +x).
#   - Hard-blocks overwriting any existing database (no WITH REPLACE).
# ==============================================================================

set -euo pipefail
set +x

SCRIPT_NAME="$(basename "$0")"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-}"
CONTAINER_NAME="${MSSQL_CONTAINER:-}"
BACKUP_DIR="${MSSQL_BACKUP_DIR:-/var/opt/mssql-backups}"
BACKUP_FILE=""
USE_LATEST=0
TARGET_DB=""
KEEP_RESTORED_DB=0
CREATED_TEMP_DB=0

usage() {
  cat <<EOF
Usage: $SCRIPT_NAME [OPTIONS]

Options:
  --backup-file <file.bak>  Backup filename (in --backup-dir) or absolute path in container
  --latest                  Automatically select the newest .bak file in --backup-dir
  --container <name>        Target MSSQL container name (auto-detected from Compose if omitted)
  --compose-file <path>     Compose file for container discovery (default: docker-compose.prod.yml)
  --env-file <path>         Optional .env file for Compose container discovery
  --backup-dir <path>       Backup mount path in container (default: /var/opt/mssql-backups)
  --target-db <name>        Target database name (must NOT already exist; default: disposable temp DB)
  --keep-restored-db        Keep the restored database online instead of dropping after validation
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
    --backup-file)
      BACKUP_FILE="${2:-}"
      shift 2
      ;;
    --latest)
      USE_LATEST=1
      shift
      ;;
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
    --backup-dir)
      BACKUP_DIR="${2:-}"
      shift 2
      ;;
    --target-db)
      TARGET_DB="${2:-}"
      shift 2
      ;;
    --keep-restored-db)
      KEEP_RESTORED_DB=1
      shift
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

BACKUP_DIR="${BACKUP_DIR%/}"

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

if [[ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER_NAME" 2>/dev/null || echo false)" != "true" ]]; then
  log_error "Target MSSQL container '$CONTAINER_NAME' is not running."
  exit 1
fi

# Resolve backup file (--latest or --backup-file)
if [[ "$USE_LATEST" -eq 1 && -z "$BACKUP_FILE" ]]; then
  BACKUP_FILE="$(docker exec -u 0 "$CONTAINER_NAME" bash -c '
    ls -1t "$1"/*.bak 2>/dev/null | head -n 1 || true
  ' -- "$BACKUP_DIR")"
  if [[ -z "$BACKUP_FILE" ]]; then
    log_error "No .bak files found in '${BACKUP_DIR}' inside container '${CONTAINER_NAME}'."
    exit 1
  fi
fi

if [[ -z "$BACKUP_FILE" ]]; then
  log_error "Either --backup-file <file.bak> or --latest must be specified."
  usage >&2
  exit 2
fi

if [[ "$BACKUP_FILE" != /* ]]; then
  BACKUP_FULL_PATH="${BACKUP_DIR}/${BACKUP_FILE}"
else
  BACKUP_FULL_PATH="$BACKUP_FILE"
fi

# Generate isolated disposable database name if --target-db was not specified
if [[ -z "$TARGET_DB" ]]; then
  RAND_SUFFIX="$(od -An -N3 -tx1 /dev/urandom 2>/dev/null | tr -d ' \n' || echo "verify")"
  TARGET_DB="EgitimPlatform_RestoreVerify_$(date -u +'%Y%m%dT%H%M%SZ')_${RAND_SUFFIX}"
fi

if [[ ! "$TARGET_DB" =~ ^[A-Za-z0-9_-]+$ ]]; then
  log_error "Invalid target database name '$TARGET_DB'. Only alphanumeric, underscore, and hyphen characters are allowed."
  exit 2
fi

cleanup_temp_db() {
  if [[ "$CREATED_TEMP_DB" -eq 1 && "$KEEP_RESTORED_DB" -eq 0 ]]; then
    log_info "Cleaning up isolated disposable validation database '${TARGET_DB}'..."
    docker exec -i "$CONTAINER_NAME" bash -c '
      set +x
      export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
      /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -v TARGET_DB="$1" -i /dev/stdin >/dev/null 2>&1 || true
    ' -- "$TARGET_DB" <<'EOSQL'
DECLARE @TargetDb sysname = N'$(TARGET_DB)';
IF DB_ID(@TargetDb) IS NOT NULL
BEGIN
    DECLARE @DropSql nvarchar(max) =
        N'ALTER DATABASE ' + QUOTENAME(@TargetDb) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
        N'DROP DATABASE ' + QUOTENAME(@TargetDb) + N';';
    EXEC sp_executesql @DropSql;
END
EOSQL
    log_info "Disposable validation database '${TARGET_DB}' dropped."
  fi
}
trap cleanup_temp_db EXIT

log_info "Step 1/4: Checking backup file and SHA-256 checksum for '${BACKUP_FULL_PATH}'..."
docker exec -u 0 "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  BAK="$1"
  if [[ ! -s "$BAK" ]]; then
    echo "ERROR: Backup file does not exist or is empty: $BAK" >&2
    exit 1
  fi
  if [[ -f "${BAK}.sha256" ]]; then
    cd "$(dirname "$BAK")"
    sha256sum -c "$(basename "${BAK}.sha256")"
  else
    echo "WARN: No .sha256 sidecar found; proceeding with SQL Server CHECKSUM verification."
  fi
' -- "$BACKUP_FULL_PATH" || {
  log_error "Backup file or SHA-256 verification failed for '${BACKUP_FULL_PATH}'."
  exit 1
}

log_info "Step 2/4: Running RESTORE VERIFYONLY WITH CHECKSUM..."
docker exec -i "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  set +x
  export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -v BACKUP_PATH="$1" -i /dev/stdin
' -- "$BACKUP_FULL_PATH" <<'EOSQL'
SET NOCOUNT ON;
DECLARE @BackupPath nvarchar(1000) = N'$(BACKUP_PATH)';
DECLARE @VerifySql nvarchar(max) = N'RESTORE VERIFYONLY FROM DISK = N'
    + QUOTENAME(@BackupPath, '''')
    + N' WITH CHECKSUM, STOP_ON_ERROR;';
EXEC sp_executesql @VerifySql;
EOSQL
log_pass "RESTORE VERIFYONLY WITH CHECKSUM succeeded."

log_info "Step 3/4: Restoring into isolated database '${TARGET_DB}' (overwrite protection active)..."
# Verify target DB does NOT already exist BEFORE setting CREATED_TEMP_DB=1
docker exec -i "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  set +x
  export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -v TARGET_DB="$1" -i /dev/stdin
' -- "$TARGET_DB" <<'EOSQL'
SET NOCOUNT ON;
DECLARE @TargetDb sysname = N'$(TARGET_DB)';
IF DB_ID(@TargetDb) IS NOT NULL
BEGIN
    RAISERROR(N'SAFETY ABORT: Target database [%s] already exists. Overwriting existing databases is strictly prohibited.', 16, 1, @TargetDb);
    RETURN;
END
EOSQL

CREATED_TEMP_DB=1

docker exec -i "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  set +x
  export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b \
    -v TARGET_DB="$1" BACKUP_PATH="$2" \
    -i /dev/stdin
' -- "$TARGET_DB" "$BACKUP_FULL_PATH" <<'EOSQL'
SET NOCOUNT ON;

DECLARE @TargetDb sysname = N'$(TARGET_DB)';
DECLARE @BackupPath nvarchar(1000) = N'$(BACKUP_PATH)';

IF DB_ID(@TargetDb) IS NOT NULL
BEGIN
    RAISERROR(N'SAFETY ABORT: Target database [%s] already exists.', 16, 1, @TargetDb);
    RETURN;
END

DECLARE @FileList TABLE (
    LogicalName nvarchar(128),
    PhysicalName nvarchar(260),
    [Type] char(1),
    FileGroupName nvarchar(128),
    Size numeric(20,0),
    MaxSize numeric(20,0),
    FileID bigint,
    CreateLSN numeric(25,0),
    DropLSN numeric(25,0),
    UniqueID uniqueidentifier,
    ReadOnlyLSN numeric(25,0),
    ReadWriteLSN numeric(25,0),
    BackupSizeInBytes bigint,
    SourceBlockSize int,
    FileGroupID int,
    LogGroupGUID uniqueidentifier,
    DifferentialBaseLSN numeric(25,0),
    DifferentialBaseGUID uniqueidentifier,
    IsReadOnly bit,
    IsPresent bit,
    TDEThumbprint varbinary(32),
    SnapshotURL nvarchar(360)
);

DECLARE @FileListSql nvarchar(max) = N'RESTORE FILELISTONLY FROM DISK = N' + QUOTENAME(@BackupPath, '''') + N';';
INSERT INTO @FileList EXEC sp_executesql @FileListSql;

IF NOT EXISTS (SELECT 1 FROM @FileList)
BEGIN
    RAISERROR(N'RESTORE FILELISTONLY returned no files for backup [%s].', 16, 1, @BackupPath);
    RETURN;
END

DECLARE @MoveClauses nvarchar(max) = N'';
SELECT @MoveClauses = @MoveClauses +
    N'MOVE N' + QUOTENAME(LogicalName, '''') +
    N' TO N' + QUOTENAME(
        N'/var/opt/mssql/data/' + @TargetDb + N'_' + CAST(FileID AS nvarchar(10)) +
        CASE WHEN [Type] = 'L' THEN N'.ldf' WHEN [Type] = 'D' AND FileID = 1 THEN N'.mdf' ELSE N'.ndf' END,
        ''''
    ) + N', '
FROM @FileList
ORDER BY FileID;

DECLARE @RestoreSql nvarchar(max) =
    N'RESTORE DATABASE ' + QUOTENAME(@TargetDb) +
    N' FROM DISK = N' + QUOTENAME(@BackupPath, '''') +
    N' WITH ' + @MoveClauses +
    N'CHECKSUM, STOP_ON_ERROR, RECOVERY, STATS = 25;';

EXEC sp_executesql @RestoreSql;
EOSQL
log_pass "Isolated database '${TARGET_DB}' restored."

log_info "Step 4/4: Running DBCC CHECKDB and verifying schema/table integrity on '${TARGET_DB}'..."
docker exec -i "$CONTAINER_NAME" bash -c '
  set -euo pipefail
  set +x
  export SQLCMDPASSWORD="${MSSQL_SA_PASSWORD:-}"
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -I -b -v TARGET_DB="$1" -i /dev/stdin
' -- "$TARGET_DB" <<'EOSQL'
SET NOCOUNT ON;

DECLARE @TargetDb sysname = N'$(TARGET_DB)';

-- 1. Physical consistency check
DBCC CHECKDB (@TargetDb) WITH NO_INFOMSGS, ALL_ERRORMSGS, PHYSICAL_ONLY;

-- 2. Verify EF Core migration history and core tables exist in restored DB
DECLARE @CheckSql nvarchar(max) = N'
USE ' + QUOTENAME(@TargetDb) + N';

IF OBJECT_ID(N''dbo.__EFMigrationsHistory'', N''U'') IS NULL
BEGIN
    RAISERROR(N''Missing __EFMigrationsHistory table in restored database.'', 16, 1);
    RETURN;
END

IF OBJECT_ID(N''dbo.Institutions'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.AspNetUsers'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.AspNetRoles'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.Students'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.Exams'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.Questions'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.QuestionStyles'', N''U'') IS NULL
    OR OBJECT_ID(N''dbo.StudyPlans'', N''U'') IS NULL
BEGIN
    RAISERROR(N''One or more core platform tables are missing in restored database.'', 16, 1);
    RETURN;
END

SELECT
    (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) AS [AppliedMigrations],
    (SELECT COUNT(*) FROM dbo.AspNetRoles) AS [Roles],
    (SELECT COUNT(*) FROM dbo.Institutions) AS [Institutions],
    (SELECT COUNT(*) FROM dbo.AspNetUsers) AS [Users],
    (SELECT COUNT(*) FROM dbo.QuestionStyles) AS [QuestionStyles],
    (SELECT COUNT(*) FROM dbo.Students) AS [Students],
    (SELECT COUNT(*) FROM dbo.Exams) AS [Exams],
    (SELECT COUNT(*) FROM dbo.Questions) AS [Questions],
    (SELECT COUNT(*) FROM dbo.StudyPlans) AS [StudyPlans];
';
EXEC sp_executesql @CheckSql;
EOSQL

log_pass "Restore validation passed (DBCC CHECKDB + schema & row count verification) for '${BACKUP_FULL_PATH}'."
