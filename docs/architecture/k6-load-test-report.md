# Bilim Akademi — Pre-Launch k6 Load & Concurrency Test Report

- **Baseline Commit:** `78a09b05d116e39b5c857f13f3b28b2732c0cca7` (`main@78a09b0`)
- **Date Executed:** 2026-10-31
- **Test Script:** `tests/load/k6-prelaunch.js`
- **Dataset Seeder:** `tests/load/seed-staging-data.sql`
- **Target Concurrency:** 8 VUs (Smoke) → 150 Concurrent Virtual Users (Peak)
- **Overall Verdict:** **PASS** (All 25 pass/fail thresholds satisfied; `0.00%` HTTP failure rate across 58,064 requests at `681.99 req/s`)

---

## 1. Environment, Hardware & Isolated Staging Dataset

### 1.1 Tested Hardware & Container Topology
- **Host Hardware:** Apple M5 (`arm64`, Darwin 27.0.0), 10 CPU cores, 16 GB RAM
- **Docker Engine:** Docker `29.9.1` (`7.748 GiB` memory limit allocated to Linux VM)
- **Isolated Disposable Stack (`egitim-k6-staging` via `docker-compose.prod.yml`):**
  - `gateway`: `caddy:2-alpine` (HTTPS termination + security headers + reverse proxy)
  - `api`: `egitim-platform-api:latest` (.NET 10 ASP.NET Core Production runtime, non-root `APP_UID`)
  - `web`: `egitim-platform-web:latest` (`nginxinc/nginx-unprivileged:1.27-alpine`)
  - `mssql`: `mcr.microsoft.com/mssql/server:2022-latest` (SQL Server 2022 Developer Edition)
- **Load Generator:** `grafana/k6:latest` attached directly to the isolated `egitim-k6-staging_egitim_internal` bridge network.
- **Data Isolation & Teardown:** Executed exclusively against disposable database `EgitimPlatformStagingK6Db` and isolated tenant `11111111-2222-3333-4444-666666666666`. All containers, networks, and volumes were destroyed via `docker compose -p egitim-k6-staging -f docker-compose.prod.yml down -v` immediately after test completion.

### 1.2 Staging Dataset Size (`tests/load/seed-staging-data.sql`)
| Entity / Table | Seeded Row Count | Notes |
|---|---:|---|
| `Institutions` | 1 | `Bilim Akademi Staging (k6 Load Test)` |
| `AspNetUsers` (`@k6.staging`) | 151 | 105 Students, 20 Teachers, 15 Coaches, 10 InstitutionAdmins, 1 Bootstrap |
| `Students` | 105 | Dedicated 1:1 per Student VU (`student.vu1`..`student.vu105`) |
| `Teachers` | 20 | Dedicated 1:1 per Teacher VU (`teacher.vu1`..`teacher.vu20`) |
| `Coaches` | 15 | Dedicated 1:1 per Coach VU (`coach.vu1`..`coach.vu15`) |
| `StudentCoachAssignments` | 105 | 7 dedicated students assigned per Coach |
| `Classrooms` / `ClassroomStudents` | 6 / 105 | 17–18 students per classroom; 20 `ClassroomTeachers` links |
| `AcademicSubjects` / `AcademicTopics` | 4 / 8 | TYT Mathematics, Turkish, Physics, Chemistry |
| `Questions` / `QuestionOptions` | 40 / 160 | Published multi-choice questions across all 8 topics |
| `Exams` / `ExamSections` | 6 / 24 | 6 published TYT exams, 4 subject sections each |
| `StudentExamAttempts` | 630 | 105 students × 6 finalized exams |
| `StudentExamSubjectResults` | 2,520 | 630 attempts × 4 sections |
| `StudentExamTopicResults` | 5,040 | 630 attempts × 8 topics |
| `StudyPlans` / `StudyTasks` | 105 / 420 | 1 active plan + 4 tasks per student |
| `CoachTasks` / `Notifications` | 105 / 105 | 1 coach task + 1 unread notification per student |

---

## 2. Scenarios & Concurrency Model

To prevent shared-account contention, unrealistic per-request login overhead, and unintended login rate-limit (`429`) failures, `setup()` authenticates each virtual user's dedicated staging account once prior to stage ramp-up and maps each VU (`exec.vu.idInTest`) deterministically to its own account token.

| Scenario Name | Role | Smoke VUs | Peak VUs | Traffic Share | Exercised Endpoints (Read + Safe Isolated Write) |
|---|---|---:|---:|---:|---|
| `student_portal` | `Student` | 4 | **105** | 70.0% | **Reads:** `GET /api/v1/students/{id}/360`, `/academic-intelligence/subjects`, `/academic-intelligence/topics`, `/academic-intelligence/recommendations`, `/academic-intelligence/knowledge-map`, `/study-plans/active`, `/exam-results`, `/api/v1/notifications/unread-count`<br>**Isolated Write:** `POST /api/v1/notebook` (`201`) → `DELETE /api/v1/notebook/{noteId}` (`204`) |
| `teacher_portal` | `Teacher` | 2 | **20** | 13.3% | **Reads:** `GET /api/v1/teachers/dashboard`, `GET /api/v1/classrooms`, `GET /api/v1/questions?page=1&pageSize=20`, `GET /api/content/materials/teacher`, `GET /api/v1/academic/subjects?page=1&pageSize=50` |
| `coach_portal` | `Coach` | 1 | **15** | 10.0% | **Reads:** `GET /api/v1/students?page=1&pageSize=20`, `GET /api/v1/students/{assignedId}/360`, `GET /api/v1/students/{assignedId}/coach-tasks`, `GET /api/v1/coaching/students/{assignedId}/notes`, `GET /api/v1/coaching/meetings`<br>**Isolated Write:** `POST /api/v1/coaching/students/{assignedId}/notes` (`201`) → `DELETE /api/v1/coaching/notes/{noteId}` (`204`) |
| `admin_portal` | `InstitutionAdmin` | 1 | **10** | 6.7% | **Reads:** `GET /api/v1/institution/overview`, `GET /api/v1/reporting/institution`, `GET /api/v1/reporting/institution?classroomId={id}`, `GET /api/v1/exams?page=1&pageSize=20`, `GET /api/v1/classrooms`, `GET /api/v1/coaching/coaches` |
| **Total** | **All 4 Portals** | **8** | **150** | **100.0%** | **Ramp Profile (Peak):** `15s` ramp to 72 VUs → `20s` ramp to 150 VUs → `35s` sustained at 150 VUs → `10s` ramp-down |

---

## 3. Measured Results

### 3.1 Smoke Load Test (`TEST_MODE=smoke` — 8 VUs, 20s)
- **Total Requests (`http_reqs`):** `1,080` (`49.73 req/s`)
- **HTTP Failure Rate (`http_req_failed`):** `0.00%` (`0 / 1,080`)
- **Business Check Pass Rate (`checks`):** `100.00%` (`1,071 / 1,071`)
- **Overall `http_req_duration`:** `p50 = 4.23ms`, `p95 = 43.77ms`, `p99 = 177.08ms` (`avg = 11.81ms`, `max = 390.63ms`)

### 3.2 Peak Load Test (`TEST_MODE=peak` — 150 Concurrent VUs, 80s)
- **Total Requests (`http_reqs`):** `58,064`
- **Sustained Throughput:** `681.99 req/s` (`85.58 iterations/s`, `179 MB` received)
- **HTTP Failure Rate (`http_req_failed`):** `0.00%` (`0 / 58,064`)
- **Business Check Pass Rate (`checks`):** `100.00%` (`57,913 / 57,913`)
- **SQL / API Exceptions in Logs:** `0` (`Zero API/SQL errors found in API container logs`)

| Metric / Scenario Tag | p50 (`med`) | p90 | p95 | p99 | Avg | Max | Thresholds (`p50 / p95 / p99`) | Status |
|---|---:|---:|---:|---:|---:|---:|---|---|
| **Overall (`http_req_duration`)** | **9.79 ms** | 54.37 ms | **86.30 ms** | **203.03 ms** | 22.87 ms | 802.14 ms | `<250ms / <800ms / <1500ms` | **PASS** |
| **Student (`student_req_duration`)** | **9.96 ms** | 57.35 ms | **93.21 ms** | **216.06 ms** | 24.03 ms | 802.14 ms | `<250ms / <750ms / <1400ms` | **PASS** |
| **Teacher (`teacher_req_duration`)** | **7.44 ms** | 44.85 ms | **66.09 ms** | **131.39 ms** | 18.17 ms | 404.74 ms | `<300ms / <850ms / <1500ms` | **PASS** |
| **Coach (`coach_req_duration`)** | **7.84 ms** | 39.33 ms | **59.90 ms** | **118.07 ms** | 16.69 ms | 502.38 ms | `<250ms / <750ms / <1400ms` | **PASS** |
| **Admin (`admin_req_duration`)** | **13.49 ms** | 51.93 ms | **80.39 ms** | **179.87 ms** | 23.26 ms | 462.14 ms | `<350ms / <1000ms / <1800ms` | **PASS** |
| **Isolated Writes (`write_req_duration`)** | **11.57 ms** | 45.15 ms | **65.20 ms** | **125.35 ms** | 19.99 ms | 502.38 ms | `<300ms / <900ms / <1600ms` | **PASS** |
| **Failure Rate (`http_req_failed`)** | — | — | — | — | **0.00%** | — | `rate < 0.01 (< 1%)` | **PASS** |
| **Assertions (`checks`)** | — | — | — | — | **100.00%** | — | `rate > 0.99 (> 99%)` | **PASS** |

---

## 4. Container CPU & Memory Telemetry (150-VU Peak)

Sampled every 5 seconds via `docker stats --no-stream` across ramp-up, 150-VU sustained peak, and ramp-down:

| Container | Idle / Baseline Memory | Peak CPU % (at 150 VUs) | Sustained CPU % (150 VUs) | Peak Memory Usage | Post-Ramp-Down Memory |
|---|---:|---:|---:|---:|---:|
| `egitim-k6-staging-mssql-1` | `1.179 GiB` | **420.87%** (~4.2 cores) | `266.89% – 377.50%` | `1.299 GiB` (`16.76%`) | `1.297 GiB` |
| `egitim-k6-staging-api-1` | `195.3 MiB` | **189.52%** (~1.9 cores) | `142.40% – 176.58%` | `278.2 MiB` (`3.51%`) | `207.8 MiB` |
| `egitim-k6-staging-gateway-1` | `21.64 MiB` | **40.79%** (~0.4 cores) | `33.10% – 36.97%` | `47.86 MiB` (`0.60%`) | `42.55 MiB` |
| `egitim-k6-staging-web-1` | `14.33 MiB` | **0.78%** | `0.00%` | `14.33 MiB` (`0.18%`) | `13.35 MiB` |

---

## 5. Evidence-Based Bottleneck Analysis (`sys.dm_exec_query_stats`)

Inspection of SQL Server `sys.dm_exec_query_stats` immediately after the 58,064-request peak test identified the top resource consumers:

| Rank | Query / Endpoint | Exec Count | Total CPU (ms) | Avg CPU (ms) | Avg Elapsed (ms) | Bottleneck Analysis |
|---:|---|---:|---:|---:|---:|---|
| 1 | `GetTeacherDashboardHandler` (`WITH TeacherStudentIds AS (...)` topic weak-areas & exam aggregations) | 1,112 | `30,358.22` (combined) | `27.300` | `35.472` | **Primary SQL CPU Consumer:** Even after single-roundtrip consolidation, aggregating `StudentExamTopicResults` across classroom students per dashboard load accounts for the largest share of MSSQL worker time (`~30.4s` CPU across 1,112 calls). |
| 2 | `GetInstitutionOverviewHandler` (`WITH SubjectBuckets AS (...)`) | 369 | `4,596.22` | `12.456` | `15.534` | **Secondary SQL Aggregation Consumer:** Scans institution-wide finalized exam attempts and subject buckets (`15.53ms` avg elapsed). |
| 3 | `AcademicIntelligence` (`SELECT [s].[Id], [s].[Blank], [s].[Correct]...` from subject/topic results) | 15,426 | `4,018.27` | `0.260` | `0.262` | **Repeated Per-Widget Loading:** When a student loads the Academic portal, 4–5 separate endpoints (`subjects`, `topics`, `recommendations`, `knowledge-map`) independently query the same student's exam attempt history. |
| 4 | `InstitutionalReportHandler` (`SELECT c.Id, c.Name...` & `SELECT sub.Name AS SubjectName, AVG(sr.Net)...`) | 369 | `6,848.39` (combined) | `18.559` | `22.878` | **Reporting Aggregations:** Unfiltered and classroom-filtered institutional reports average `~22.9ms` SQL time per request. |
| 5 | Ownership & Tenant Resolution (`SELECT TOP(2) ... FROM [Students]` + `SELECT TOP(1) [InstitutionId] FROM [AspNetUsers]`) | 125,534 (combined) | `5,617.76` (combined) | `0.045` | `0.046` | **High-Frequency Lookup Chattiness:** Executed `72,806` and `52,728` times respectively during the 80s run (`~1,569 lookups/sec`) due to per-request student ownership and user-institution verification. |

### Key Conclusions & Future Optimization Candidates (Non-Blocking for Launch)
1. **Database CPU is the primary scaling ceiling:** At 150 concurrent VUs (`682 req/s`), `mssql` reached `420.87%` CPU while `api` peaked at `189.52%` CPU and `278.2 MiB` RAM. Provisioning at least 4 dedicated vCPUs and 4–8 GB RAM for MSSQL is recommended for production.
2. **Short-TTL Output/Memory Caching (15–30s) on Dashboard & Academic Intelligence Endpoints:** Adding a 15–30 second per-tenant/per-user cache on `GET /api/v1/teachers/dashboard`, `GET /api/v1/institution/overview`, and student `academic-intelligence/*` endpoints would reduce MSSQL CPU utilization by >60% under burst concurrency.
3. **Request-Scoped / Claim-Backed Student Ownership Resolution:** Caching the resolved `(UserId → StudentId, InstitutionId)` mapping per request or in a short-lived memory cache would eliminate >120,000 repetitive point lookups per 58k requests.
