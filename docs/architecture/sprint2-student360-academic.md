# Sprint 2: academic foundation and Student360

## Ownership and composition

Infrastructure owns ApplicationDbContext and all migrations. Academic and Teachers are real solution/API dependencies; missing module configuration assemblies fail startup/model creation. Cross-module queries use BuildingBlocks contracts instead of module references.

Academic reference data is shared across institutions. The hierarchy is ExamType -> Subject -> Topic -> SubTopic -> LearningOutcome -> Concept. Each level has a globally unique code within its own table, including deleted records. Codes and parent identities cannot be rewritten through normal SaveChanges workflows. Hard deletion is rejected; read queries hide deleted nodes and descendants of deleted ancestors.

## API and authorization

All routes use `/api/v1` and DTOs. Resource checks follow authentication and coarse role policies.

- `GET /students/{studentId}/360`: student profile, academic profile, active goals, safe coach summaries, parent relationships and referenced ExamTypes. No fabricated analytics or future data.
- Student: own institution and own resource only.
- InstitutionAdmin: own institution only. SuperAdmin: explicit platform-wide access.
- Coach: same institution and active student assignment.
- Parent: same institution and active, nondeleted StudentParent and Parent. The composed response omits goal descriptions/history, private relationship notes and parent contact details; a parent sees only their own parent relationship. Goal mutations/history remain unavailable to Parent.
- Teacher: student access denied because no TeacherStudent relationship exists. Subject assignment alone never grants student access.
- `POST /parents`, `POST /parents/relationships`, `PUT /parents/relationships/{id}`: institution administrators or SuperAdmin, with resource institution checks.
- `POST /teachers`, `POST` or `DELETE /teachers/{teacherId}/subjects/{subjectId}`: institution administrators or SuperAdmin. `GET /teachers/{teacherId}/subjects`: the same administrators or that teacher within their institution.
- `GET /academic/{level}?parentId=...&page=1&pageSize=100`: authenticated shared lookup. Levels: exam-types, subjects, topics, sub-topics, learning-outcomes, concepts. Maximum page size 200.

Global query filters retain the existing soft-delete convention. Tenant safety combines explicit resource authorization/query predicates with composite database constraints; shared taxonomy intentionally has no InstitutionId. Parent/Teacher account creation additionally validates the active Identity account, institution and required role.

## Persistence and history

Migration `20260906183807_Sprint2AcademicStudent360` belongs to Infrastructure. StudentParent references Student and Parent with composite identity/institution FKs. TeacherSubject references Teacher with an institution-safe composite FK and real Subject with an FK. StudentGoal references real ExamType. Unique live/active relationship indexes prevent duplicates. Restrict/NO ACTION relationships avoid cascading tenant data deletion. StudentGoal and StudentParent have rowversion concurrency.

Goal history snapshots include complete goal state, exam identity, actor, time and correlation. SaveChanges rejects history updates/deletes. This is application-level protection, not protection against privileged raw SQL. Critical relationship/goal changes use the existing audit service and transaction path. Client database failures are sanitized.

Before applying the migration to an existing database, check legacy non-null TargetExamTypeId values against the verified taxonomy. Arbitrary legacy IDs will correctly block FK creation; no destructive cleanup or invented reference records are included. Applying this migration to a live database has not been verified in this environment. Review rollback separately: Down removes the new schema and narrows goal-history storage.

## Verified seed manifest

No production curriculum dataset is supplied. Test records are synthetic fixtures only.

Provide `Academic__VerifiedSeedManifest` as a path to a reviewed JSON array of records with `Level`, explicit `Id` (GUID), stable uppercase `Code`, `Name`, and `ParentId` (null only for ExamType). Run the existing explicit initialization command with `--initialize-platform`; normal startup does not seed taxonomy. Initialization also runs the existing platform initialization path, so configure its existing required settings.

The seeder validates records, processes parents first and saves once. Repeating an identical manifest is a no-op. Conflicting IDs, codes, names or parent relationships fail rather than silently overwriting reference data. This mechanism establishes identity; it does not certify curriculum content.

## Review and verification

New tests cover resource access, history protection, taxonomy model/query translation, real MSSQL FKs and concurrency, parent response safety and HTTP authorization across roles/institutions. Relational tests use Testcontainers MSSQL, never EF InMemory.

The implementation environment cannot start Docker Desktop successfully. Consequently MSSQL integration/security tests are not verified; fixture failures must not be reported as successful assertions. A separate reviewer must run the full Release suite with a working Docker engine and inspect migration application before independent approval. No independent approval is asserted by the implementer.
