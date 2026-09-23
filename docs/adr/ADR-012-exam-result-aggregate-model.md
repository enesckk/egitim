# ADR-012: Exam Result Aggregate Model and Scoring Lifecycle

## Status
Accepted

## Context
Sprint 3 introduces exam management (Deneme Sınavları), student exam attempts, and subject/topic performance result analysis. High-throughput EdTech systems require fast result analysis without forcing raw question-level persistence during early aggregate reporting phases.

## Decision

1. **Aggregate-First Engine**:
   Sprint 3 canonical domain model stores data at the section and subject/topic aggregate level:
   - `Exam`
   - `ExamSection`
   - `StudentExamAttempt`
   - `StudentExamSubjectResult`
   - `StudentExamTopicResult`

2. **Future Hybrid Extensibility**:
   Future additions (`ExamQuestion`, `StudentExamQuestionResult`, OCR, PDF import, Question Bank, Knowledge Map) will extend this architecture as optional detail layers without altering or breaking Sprint 3 aggregate results.

3. **Deterministic Net Scoring Formula**:
   Backend calculates Net scores using precise `decimal` arithmetic:
   $$\text{Net} = \text{Correct} - \frac{\text{Wrong}}{\text{Penalty}}$$
   Default penalty is 4 (e.g., 4 wrong answers penalize 1 correct answer). Client-supplied Net values are strictly ignored.

4. **Score Handling**:
   Official ÖSYM score calculation engines are not derived. Attempts store only nullable `ReportedScore` and explicit `ScoreSource` (`None`, `Institution`, `Provider`).

5. **Attempt Lifecycle & Immutability**:
   Attempts transition from `Draft` to `Finalized`. Finalized attempts and their child subject/topic results are immutable over standard business APIs. Attempting to update or delete a finalized attempt returns HTTP status `409 Conflict`.

6. **Security & Tenant Isolation**:
   - Foreign/Missing tenant resources return `404 Not Found` to prevent resource enumeration.
   - Unauthorized access within the same tenant returns `403 Forbidden`.
   - Anonymous requests return `401 Unauthorized`.
   - Teacher role is FAIL CLOSED for student exam result access (subject assignments do not grant student exam result visibility).

## Consequences

**Positive:**
- High performance for student & coach dashboard queries.
- Clean separation between aggregate performance metrics and granular question banks.
- Strict tenant safety via composite database foreign keys.

**Negatives:**
- Granular per-question item analysis is unavailable until future question-level hybrid extensions are introduced.
