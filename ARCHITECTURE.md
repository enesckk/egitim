# ARCHITECTURE — Egitim Platformu

## Genel Mimari Yaklaşım

**Modular Monolith** — tek deployment birimi, modüler iç yapı.

Microservice değil çünkü:
- Erken aşama ürün için operational overhead gereksiz
- Tek codebase, tek deploy, tek CI/CD pipeline
- Modüller arası sınır net tutularak gelecekte microservice'e geçiş mümkün

**Clean Architecture principles** — katmanlar arası bağımlılık yönü dıştan içe.

**Vertical Slice Architecture** — her özellik (feature) kendi dikey dilimini oluşturur: endpoint → validator → handler → data access.

**Light CQRS** — Command/Query ayrımı, MediatR zorunlu değil, in-process dispatcher yeterli.

**DDD only where useful** — Entity, Value Object, Aggregate kavramları iş kurallarının yoğun olduğu yerlerde uygulanır. Her yerde DDD zorunlu değil.

## Katman Yapısı

```
src/
  Api/                          → ASP.NET Core host (composition root)
    EgitimPlatform.Api/
  BuildingBlocks/               → Ortak altyapı (validation, exceptions, abstractions)
    EgitimPlatform.BuildingBlocks/
  Modules/
    Identity/                   → Her modül kendi içinde dikey slice
    Institutions/
    Students/
    Coaching/
    Academic/
    Questions/
    Exams/
    Learning/
    Planning/
    Content/
    Communication/
    Notebook/
    Reporting/
    AI/
  Web/                          → React + TypeScript + Vite + PWA (frontend)
  AIService/                    → Ayrı AI servisi (ileri aşama)
```

### Modül İç Yapısı (Vertical Slice)

Her modül kendi içinde şu yapıyı izler:

```
Modules.{ModuleName}/
  Features/
    {FeatureName}/
      Commands/
        CreateSomething/
          CreateSomethingCommand.cs
          CreateSomethingCommandHandler.cs
          CreateSomethingValidator.cs
      Queries/
        GetSomething/
          GetSomethingQuery.cs
          GetSomethingQueryHandler.cs
  Domain/
    Entities/
    ValueObjects/
    Events/
  Infrastructure/
    Persistence/
    Data/
```

## Bağımlılık Yönü

```
Api → Modules → BuildingBlocks
```

- **Api**, tüm modülleri referans alır ve compose eder.
- **Modüller** BuildingBlocks'taki ortak araçları kullanır.
- **Modüller** birbirini doğrudan referans almaz — modüller arası iletişim domain event veya public contract üzerinden.
- **BuildingBlocks** hiçbir modüle bağımlı değildir.

## YASAK Kuralları

| Yasak | Sebep |
|-------|-------|
| `GenericRepository<T>` | Abstraction over abstraction. EF DbContext zaten repository. |
| Gereksiz `UnitOfWork` | EF DbContext Unit of Work'tur. Ek wrapping gereksiz. |
| Premature microservice | Tek ekip, tek ürün — microservice overhead gereksiz. |
| EF entity'yi API response olarak dönmek | DTO boundary ihlali. Lazy loading risk. |
| Modüller arası doğrudan referans | Coupling. Contract/event kullanılmalı. |

## Modüller Arası İletişim

1. **In-process Domain Events** — aynı process'te, senkron.
2. **Public Contracts** — modülün diğer modüllere açtığı interface/DTO.
3. **Shared Kernel (BuildingBlocks)** — gerçekten ortak olan araçlar.

## Authorization Mimarisi

- ASP.NET Core Identity temel auth
- Custom Authorization Policy'leri
- Resource-based authorization handler'ları
- InstitutionId her query'de otomatik filtre (global query filter)
- Ownership check: user → role → institution → resource

## Database Mimarisi

- **MSSQL** (Microsoft SQL Server)
- EF Core Code-First
- Her modülün kendi DbContext'i olabilir veya shared DbContext (modül bazlı DbSet grouping)
- Migration'lar Backend Lead tarafından yönetilir
- Soft-delete varsayılan (IsDeleted + DeletedAt)
- Audit fields: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

## API Mimarisi

- ASP.NET Core Minimal API veya Controllers
- `/api/v1/{module}/{resource}` URL pattern
- FluentValidation pipeline behavior
- ProblemDetails standard error response
- Pagination: cursor-based veya offset-based (endpoint bazında karar)
- OpenAPI / Swagger otomatik dokümantasyon
- JWT access token + refresh token

## Background Jobs

- **Hangfire** + MSSQL storage
- Email gönderimi, rapor hesaplama, bildirim üretimi gibi async işler
- Dashboard: `/hangfire` (development only, production authenticated)

## Real-time Communication

- **SignalR**
- Bildirimler, canlı sınav durumu, mesajlaşma

## Logging

- **Serilog** → MSSQL sink veya file sink
- Structured logging
- Correlation ID her request'te
- Sensitive data (password, token, PII) loglanmaz

## Testing Mimarisi

```
tests/
  Unit/                  → Domain logic, validators, handlers
  Integration/           → API + DB (Testcontainers ile MSSQL)
  Security/              → Authorization, institution isolation
  E2E/                   → Playwright browser tests
```

## Frontend Mimarisi

- **React + TypeScript + Vite**
- PWA desteği
- `src/Web/` altında bağımsız proje
- API ile sözleşme bazlı iletişim (OpenAPI client generation)
- State management: TBD (mimari aşamasında belirlenecek)

## Deployment

- Her kurum için isolated instance
- Docker container (production)
- Environment-based configuration
- Database per tenant (opsiyonel: shared DB + InstitutionId filter)
