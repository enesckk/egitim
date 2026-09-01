# ADR-008: Hangfire Background Jobs

## Status
Accepted

## Context
Background job gereksinimleri:
- Email gönderimi (welcome, password reset, notification)
- Rapor hesaplama (günlük, haftalık, aylık)
- Bildirim üretimi
- Data cleanup (expired tokens, soft-deleted records)
- Scheduled tasks (cron jobs)

Seçenekler:
1. BackgroundService (.NET built-in)
2. Quartz.NET
3. Hangfire
4. Azure Functions / AWS Lambda

## Decision
**Hangfire + MSSQL storage** benimsendi.

### Neden Hangfire?
- **Persistent storage** — MSSQL ile job'lar kalıcı, restart-safe
- **Dashboard** — web UI ile job monitoring, retry, cancel
- **Multiple job types:**
  - Background (fire-and-forget)
  - Delayed (gelecekte çalışacak)
  - Recurring (cron-based)
- **Retry mechanism** — otomatik retry, manual retry
- **Distributed** — birden fazla worker instance mümkün

### Job Örnekleri
```csharp
// Fire-and-forget
BackgroundJob.Enqueue(() => SendWelcomeEmail(userId));

// Delayed
BackgroundJob.Schedule(() => SendReminder(studentId), TimeSpan.FromDays(1));

// Recurring
RecurringJob.AddOrUpdate("daily-report", () => GenerateDailyReport(), Cron.Daily);
```

### Dashboard
- Development: `/hangfire` (anonymous erişim)
- Production: `/hangfire` (authenticated, SuperAdmin only)

### MSSQL Storage
- Hangfire metadata, job state, history MSSQL'de saklanır
- Aynı database veya ayrı Hangfire database mümkün
- Transaction desteği — job execution atomic

## Consequences

**Pozitif:**
- Reliable job execution — restart-safe
- Easy monitoring — dashboard
- Cron job yönetimi kolay
- Retry mechanism built-in

**Negatif:**
- MSSQL storage overhead — ekstra tablolar
- Dashboard security — production'da dikkatli
- Job serialization — dependency injection karmaşık

**Mitigation:**
- Hangfire tabloları ayrı schema'da tutulur
- Production dashboard strict authorization
- Job parameter'ları basit tutulur (ID, complex object değil)
- Job timeout ve cancellation token desteği

**Alternatif değerlendirme:**
- BackgroundService: Basit ama persistent değil, restart'ta kaybolur
- Quartz.NET: Powerful ama karmaşık, dashboard yok
- Azure Functions: Serverless ama vendor lock-in, cost
