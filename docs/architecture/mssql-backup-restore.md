# MSSQL Production Backup, Verification & Restore Safety Runbook

This document defines the production backup architecture, verification procedures, isolated restore validation workflow, security controls, and recovery limitations for **Eğitim Platformu** (`SQL Server 2022`).

---

## 1. Storage Architecture & Volume Isolation

In [`docker-compose.prod.yml`](../../docker-compose.prod.yml), the `mssql` service mounts two strictly separated persistent volumes:

| Volume Name | Container Mount Path | Purpose |
|-------------|----------------------|---------|
| `mssql_data` | `/var/opt/mssql` | Active SQL Server database files (`.mdf`, `.ndf`, `.ldf`), system databases (`master`, `msdb`, `model`, `tempdb`). |
| `mssql_backups` | `/var/opt/mssql-backups` | Dedicated backup volume for `.bak` archives and `.bak.sha256` checksum manifests. |

### Safety Enforcement
- [`scripts/mssql-backup.sh`](../../scripts/mssql-backup.sh) **rejects** any `--backup-dir` pointing to `/var/opt/mssql` or any subdirectory under `/var/opt/mssql/*`.
- Before writing any backup, the script verifies via `/proc/mounts` that `/var/opt/mssql-backups` is an active mounted volume inside the container rather than ephemeral container rootfs storage.

---

## 2. File & Directory Permissions

SQL Server 2022 Linux containers (`mcr.microsoft.com/mssql/server:2022-latest`) execute `sqlservr` as non-root user `mssql` (`UID 10001`, `GID 10001`).

- **Backup Directory (`/var/opt/mssql-backups`)**: Owned by `10001:10001` (`mssql:mssql`) with mode `0750` (`rwxr-x---`).
- **Backup Artifacts (`*.bak` and `*.bak.sha256`)**: Owned by `10001:10001` with mode `0640` (`rw-r-----`).
- **Credential Hygiene**:
  - Both [`scripts/mssql-backup.sh`](../../scripts/mssql-backup.sh) and [`scripts/mssql-restore-verify.sh`](../../scripts/mssql-restore-verify.sh) enforce `set +x` and authenticate to `sqlcmd` via the in-container `SQLCMDPASSWORD` environment variable rather than passing `-P` on the command line (preventing credential exposure in `ps` output, shell history, or container logs).

---

## 3. Backup & Verification Workflow

### 3.1 Taking a Full Verified Backup

Run [`scripts/mssql-backup.sh`](../../scripts/mssql-backup.sh) from the repository root:

```bash
./scripts/mssql-backup.sh --env-file .env.production
```

What the script performs:
1. Connects to the running `mssql` container without exposing `MSSQL_SA_PASSWORD`.
2. Detects SQL Server edition capabilities and executes:
   ```sql
   BACKUP DATABASE [EgitimPlatform]
   TO DISK = N'/var/opt/mssql-backups/EgitimPlatform_<UTC_TIMESTAMP>.bak'
   WITH FORMAT, INIT, CHECKSUM, STOP_ON_ERROR, STATS = 25, COMPRESSION;
   ```
   *(If running on SQL Server Express where `COMPRESSION` is unsupported, the script automatically omits `COMPRESSION` while retaining `CHECKSUM` and `STOP_ON_ERROR`.)*
3. Generates a companion SHA-256 checksum manifest (`<backup>.bak.sha256`).
4. Executes `RESTORE VERIFYONLY FROM DISK = ... WITH CHECKSUM, STOP_ON_ERROR` to verify page checksums and backup header/media integrity.
5. Applies local retention cleanup (`--retention-days`, default `14`) **only after** the new backup and `RESTORE VERIFYONLY WITH CHECKSUM` succeed.

### 3.2 Standalone Verification of an Existing Backup

To re-verify an existing `.bak` file (both SHA-256 checksum and `RESTORE VERIFYONLY WITH CHECKSUM`) without taking a new backup:

```bash
./scripts/mssql-backup.sh --env-file .env.production --verify-only EgitimPlatform_20261030T061500Z.bak
```

---

## 4. Safe Isolated Restore Validation

Never test or validate backups by restoring over a live database. [`scripts/mssql-restore-verify.sh`](../../scripts/mssql-restore-verify.sh) validates backup recoverability using an **isolated disposable database** (or a separate disposable container) and hard-blocks overwriting any existing database.

### 4.1 Validating the Latest Backup

```bash
./scripts/mssql-restore-verify.sh --env-file .env.production --latest
```

What the script performs:
1. Verifies the `.bak` file and its `.bak.sha256` sidecar checksum.
2. Executes `RESTORE VERIFYONLY FROM DISK = ... WITH CHECKSUM, STOP_ON_ERROR`.
3. Queries `RESTORE FILELISTONLY` and dynamically maps all logical data and log files (`MOVE '<LogicalName>' TO '/var/opt/mssql/data/<DisposableDbName>_<FileId>.mdf/.ldf'`) so physical file paths never collide with the active database.
4. Verifies `DB_ID(@TargetDb) IS NULL` (aborting immediately if the target database name already exists; `WITH REPLACE` is never used).
5. Restores the backup into `EgitimPlatform_RestoreVerify_<UTC_TIMESTAMP>_<HEX>`.
6. Runs `DBCC CHECKDB ([...]) WITH NO_INFOMSGS, ALL_ERRORMSGS, PHYSICAL_ONLY` and validates the presence and row counts of `__EFMigrationsHistory`, `Institutions`, `AspNetUsers`, `AspNetRoles`, `QuestionStyles`, `Students`, `Exams`, `Questions`, and `StudyPlans`.
7. Automatically drops the disposable validation database in an `EXIT` trap so no stale validation data remains.

### 4.2 Restoring into a New Target Database (Disaster Recovery)

To restore a verified backup into a brand-new database name (or onto a fresh SQL Server instance where `EgitimPlatform` does not yet exist) and keep the restored database online:

```bash
./scripts/mssql-restore-verify.sh \
  --env-file .env.production \
  --backup-file EgitimPlatform_20261030T061500Z.bak \
  --target-db EgitimPlatform \
  --keep-restored-db
```

> **Safety Constraint:** If `--target-db` already exists on the SQL Server instance, `mssql-restore-verify.sh` aborts with `SAFETY ABORT` and refuses to overwrite it.

---

## 5. Retention Policy & Off-Host Backup Requirements

### 5.1 Local vs. Off-Host Retention Tiers

| Tier | Storage Location | Retention | Purpose |
|------|------------------|-----------|---------|
| **Tier 1 (Local Hot)** | `mssql_backups` volume (`/var/opt/mssql-backups`) | **14 days** (`BACKUP_RETENTION_DAYS=14`) | Fast local rollback before migrations or operational changes. |
| **Tier 2 (Off-Host Daily)** | Encrypted off-host object storage / remote backup server | **30 days** | Protection against host disk failure, accidental volume deletion, or host compromise. |
| **Tier 3 (Off-Host Weekly/Monthly)** | Immutable / WORM archive bucket | **90–365 days** | Compliance, academic term history, and ransomware resilience. |

### 5.2 Why Off-Host Replication is Mandatory

Storing backups in the `mssql_backups` volume protects against container recreation and separates backup I/O from `/var/opt/mssql`, **but `mssql_backups` still resides on the same physical host**. A host disk failure, host-level `docker volume rm`, or host ransomware incident would affect both `mssql_data` and `mssql_backups`.

Production operators **must** schedule an off-host replication step immediately after `scripts/mssql-backup.sh` completes (e.g., copying encrypted `.bak` and `.bak.sha256` files to a separate backup host or S3-compatible object storage with Object Lock enabled).

---

## 6. Encryption Considerations

1. **In-Transit Encryption**:
   - Application and `sqlcmd` connections use `Encrypt=True` (`-C` for local loopback container socket).
   - Off-host backup transfers must use TLS/SSH (`rclone`, `restic`, `aws s3 cp` over HTTPS, or `rsync` over SSH).
2. **At-Rest Encryption**:
   - Native SQL Server `BACKUP DATABASE ... WITH ENCRYPTION` requires a Database Master Key (DMK) + Server Certificate in `master` and is edition-dependent; losing the certificate makes the `.bak` unrecoverable.
   - For single-institution deployments across SQL Server editions, **envelope encryption of the `.bak` artifact prior to off-host transfer** (using `age`, `gpg --symmetric --cipher-algo AES256`, or `restic` repository encryption) with keys stored in an external secret manager is recommended, alongside host-level LUKS/volume encryption.

---

## 7. Operational & Recovery Limitations

1. **No Point-in-Time Recovery (PITR) Configured by Default**:
   - `scripts/mssql-backup.sh` performs **Full Database Backups (`BACKUP DATABASE`)** only.
   - Automated Transaction Log backups (`BACKUP LOG`) and log-chain shipping are **not** configured in this deployment phase.
   - **Impact (RPO)**: Recovery is strictly to the timestamp of the most recent verified `.bak` full backup. Any data written after the last full backup cannot be replayed via point-in-time log restore unless `FULL` recovery model and recurring `BACKUP LOG` jobs are explicitly implemented.
2. **Single-Host Volume Scope**:
   - Until an external off-host sync job (cron/systemd timer + `restic`/`rclone`) is configured on the production host, backups in `mssql_backups` remain local to the Docker host.
3. **File Storage Outside MSSQL**:
   - Database backups cover relational data in SQL Server (`EgitimPlatform`). Any local filesystem uploads must be backed up separately alongside the database backup.
