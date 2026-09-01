# AGENTS — Egitim Platformu

Bu dosya Claude Code agent'ları ve otomatik sistemler için projenin çalışma kurallarını tanımlar.

## Proje Bağlamı

- **Proje:** Egitim Platformu — EdTech SaaS/PWA
- **Stack:** .NET 10, ASP.NET Core, C#, EF Core, MSSQL, React + TypeScript + Vite
- **Mimari:** Modular Monolith, Clean Architecture, Vertical Slice, Light CQRS
- **Repository:** https://github.com/enesckk/egitim.git

## Rol Tanımları

| Agent Rolü | Sorumluluk |
|------------|-----------|
| **Lead Backend Engineer** | Mimari kararlar, modül tasarımı, code review, security review |
| **Senior Software Architect** | Sistem tasarımı, ADR yazımı, teknoloji değerlendirmesi |
| **Frontend Developer** | React + TypeScript UI geliştirme (ilerleyen aşamada) |
| **QA Engineer** | Test stratejisi, integration/security test yazımı |
| **DevOps Engineer** | CI/CD, deployment, monitoring (ilerleyen aşamada) |

## Çalışma Kuralları

### Kod Yazmadan Önce

1. **Mevcut kodu oku** — pattern'ları ve convention'ları anla.
2. **İlgili modülü incele** — dikey slice yapısına uygun mu?
3. **Authorization kontrolü** — yeni endpoint institution isolation'a uygun mu?
4. **DTO boundary** — EF entity doğrudan response değil, DTO kullan.
5. **Validation** — FluentValidation ile input validation zorunlu.

### Yasak İşlemler

- `GenericRepository<T>` oluşturma
- Gereksiz `UnitOfWork` abstraction
- EF entity'yi API response olarak döndürme
- Modüller arası doğrudan referans ekleme
- Migration dosyası oluşturma (yalnız Backend Lead yönetir)
- Source code'a secret/token/connection string yazma
- Sensitive data'yı log'a yazma

### Code Review Kontrol Listesi

- [ ] Authorization kontrolü (RBAC + policy + resource-based)
- [ ] Institution isolation (GlobalQueryFilter + explicit check)
- [ ] Input validation (FluentValidation)
- [ ] DTO kullanımı (EF entity response değil)
- [ ] Error handling (ProblemDetails)
- [ ] Logging (correlation ID, no sensitive data)
- [ ] Test coverage (unit + integration)

### Modül Yapısı

Her yeni özellik Vertical Slice pattern'ına uygun olmalı:

```
Features/
  {FeatureName}/
    Commands/
      {CommandName}/
        {CommandName}Command.cs
        {CommandName}CommandHandler.cs
        {CommandName}Validator.cs
    Queries/
      {QueryName}/
        {QueryName}Query.cs
        {QueryName}QueryHandler.cs
```

### Authorization Kontrolü

Her endpoint için şu sorulara cevap ver:

1. Hangi roller erişebilir?
2. Institution scope doğru mu?
3. Ownership check gerekli mi? (Coach → Student, Teacher → Student)
4. Cross-institution erişim engeli var mı?
5. Audit log gerekli mi?

### Test Gereksinimleri

- **Unit test:** Her handler, validator, domain rule
- **Integration test:** API endpoint + DB (Testcontainers MSSQL)
- **Security test:** Authorization bypass, institution isolation
- **E2E test:** Kritik kullanıcı akışları (Playwright)

### Database Kuralları

- Migration dosyaları yalnız Backend Lead tarafından oluşturulur
- Soft-delete varsayılan (IsDeleted + DeletedAt)
- Audit fields zorunlu: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
- Optimistic concurrency gereken yerlerde RowVersion

### Documentation Kuralları

- Yeni modül → `docs/architecture/` altına modül dokümantasyonu
- Yeni ADR → `docs/adr/` altına numaralı ADR
- API değişikliği → OpenAPI otomatik güncellenir, manual dokümantasyon gerekmez

## Security Kuralları

1. **No secrets in code** — environment variable veya secret manager kullan
2. **No sensitive data in logs** — password, token, PII loglama
3. **Input validation** — her endpoint FluentValidation ile doğrula
4. **Institution isolation** — her query'de InstitutionId filtresi
5. **Authorization** — her endpoint'te explicit yetki kontrolü
6. **Audit logging** — kritik işlemler (feedback reveal, user deletion) immutable log
7. **Rate limiting** — hazır tasarım, production'da aktif

## Bağımlılık Yönetimi

- `dotnet add package` ile yeni paket eklemeden önce değerlendir
- Güvenlik açığı olan paket kullanma
- Paket versiyonları merkezi yönetilmeli (Directory.Build.props — ileride)

## Commit Convention

```
feat({module}): add {feature}
fix({module}): fix {issue}
refactor({module}): {description}
test({module}): add {test type}
docs: {description}
chore: {description}
```

## Claude Code Araç Kullanımı

- **Context7 MCP** — framework/library dokümantasyonu
- **Playwright MCP** — UI doğrulama, E2E test
- **Impeccable** — frontend tasarım (ilerleyen aşamada)
- **frontend-design** — UI estetiği (ilerleyen aşamada)
- **code-review** — PR review
- **security-review** — güvenlik review
- **simplify** — code simplification
- **Ponytail** — over-engineering önleme
