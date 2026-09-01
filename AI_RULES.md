# AI RULES — Egitim Platformu

Bu dosya, Claude Code ve diğer AI agent'ların bu projede nasıl çalışacağını tanımlar.

## Genel Prensipler

1. **Önce analiz, sonra kod.** Büyük değişikliklerde doğrudan implementasyona geçme.
2. **Mevcut pattern'ları koru.** Dikey slice, modüler yapı, authorization kuralları.
3. **YASAK kurallarına uy.** GenericRepository, UnitOfWork, EF entity response, vs.
4. **Security first.** Authorization, institution isolation, audit logging.
5. **Ponytail prensibi.** Gereksiz abstraction yapma, en basit çözümü seç.
6. **Test et.** Kod yazdıktan sonra build, test, lint çalıştır.

## Code Generation Kuralları

### Yeni Feature Eklerken

1. **İlgili modülü belirle** (Identity, Students, Coaching, vb.)
2. **Vertical Slice pattern'ını uygula:**
   ```
   Features/
     {FeatureName}/
       Commands/ veya Queries/
         {Operation}/
           {Operation}Command.cs veya {Operation}Query.cs
           {Operation}Handler.cs
           {Operation}Validator.cs
   ```
3. **Authorization kontrolü:**
   - Hangi roller erişebilir?
   - Institution isolation var mı?
   - Ownership check gerekli mi?
4. **DTO kullan.** EF entity'yi response olarak döndürme.
5. **Validation ekle.** FluentValidation ile.
6. **Test yaz.** Unit test + integration test.

### Database Değişikliği Yaparken

- **Migration oluşturma.** Migration yalnız Backend Lead tarafından yönetilir.
- Değişikliği dokümante et → `docs/architecture/` veya ilgili modül dizini.
- Soft-delete mi hard-delete mi belirt.
- Index gereksinimi varsa belirt.

### API Endpoint Eklerken

1. **Route pattern:** `/api/v1/{module}/{resource}`
2. **HTTP method:** Doğru kullan (GET, POST, PUT, DELETE)
3. **Request DTO:** Input validation ile
4. **Response DTO:** EF entity değil
5. **Error handling:** ProblemDetails formatında
6. **OpenAPI:** Otomatik dokümantasyon
7. **Authorization:** Explicit [Authorize] attribute

## Claude Code Araç Kullanımı

### Context7 MCP

**Ne zaman:**
- .NET, EF Core, ASP.NET Core, FluentValidation, Hangfire, SignalR dokümantasyonu
- React, TypeScript, Vite dokümantasyonu (ilerleyen aşamada)
- Herhangi bir library/framework hakkında güncel bilgi

**Nasıl:**
```
1. Resolve library ID (mcp__context7__resolve-library-id)
2. Query docs (mcp__context7__query-docs)
```

### Playwright MCP

**Ne zaman:**
- UI doğrulama (ilerleyen aşamada, frontend geliştirmeye başlandığında)
- E2E test senaryoları
- Browser-based debugging

**Nasıl:**
- localhost veya 127.0.0.1 üzerinde otomatik
- Gerçek web sitesinde form submit → onay iste

### Code Review

**Ne zaman:**
- PR oluşturulduğunda
- Önemli kod değişikliğinden sonra

**Nasıl:**
- code-review skill kullan
- Authorization, institution isolation, validation kontrol et
- Security review için security-review skill kullan

### Security Review

**Ne zaman:**
- Authentication/authorization değişikliği
- Sensitive data erişimi
- External entegrasyon
- Production deployment öncesi

**Nasıl:**
- security-review skill kullan
- OWASP Top 10 kontrolü
- Audit logging kontrolü

### Ponytail

**Ne zaman:**
- Her kod yazma kararında aktif
- Over-engineering şüphesi olduğunda

**Nasıl:**
- YAGNI: Speculative need = skip
- Stdlib first: .NET BCL kütüphanesi yeterli mi?
- Existing pattern: Projede zaten var mı?
- Minimum code: En kısa working diff

## Yasak İşlemler

AI agent'ların **ASLA** yapmaması gerekenler:

1. **GenericRepository<T> oluşturma**
2. **Gereksiz UnitOfWork abstraction**
3. **EF entity'yi API response olarak döndürme**
4. **Modüller arası doğrudan referans ekleme**
5. **Migration dosyası oluşturma** (yalnız Backend Lead)
6. **Source code'a secret/token yazma**
7. **Sensitive data'yı log'a yazma**
8. **Cross-institution erişim kodu yazma**
9. **Authorization kontrolü olmadan endpoint ekleme**
10. **Test yazmadan kodu bitmiş sayma**

## Documentation Kuralları

### Ne Zaman Dokümantasyon Güncellenir

- Yeni modül eklendi → `ARCHITECTURE.md` + modül dokümantasyonu
- Mimari karar değişti → ADR ekle
- API değişti → OpenAPI otomatik güncellenir
- Security kuralı değişti → `SECURITY.md`

### ADR (Architecture Decision Record)

Yeni ADR ne zaman eklenir:
- Yeni teknoloji seçimi
- Mimari pattern değişikliği
- Modül yapısı değişikliği
- Security yaklaşımı değişikliği

ADR formatı:
```
# ADR-{NUM}: {TITLE}

## Status
{Proposed | Accepted | Deprecated | Superseded}

## Context
{Karar neden gerekli?}

## Decision
{Ne karar verildi?}

## Consequences
{Pozitif ve negatif sonuçlar}
```

## Sprint Planlama

### İlk Sprint (Mevcut)

- [x] Repository skeleton
- [x] Documentation (MASTER_PRODUCT_SPEC, ARCHITECTURE, AGENTS, SECURITY, AI_RULES)
- [x] ADR'ler
- [ ] Identity modülü implementasyonu (authentication + authorization)
- [ ] Institutions modülü implementasyonu (institution, branch, classroom, group)
- [ ] Students modülü implementasyonu (student profile, atamalar)
- [ ] Integration test altyapısı (Testcontainers MSSQL)

### Sonraki Sprint'ler

- Coaching, Academic, Planning modülleri
- Content, Communication modülleri
- Exam, Questions modülleri (ileri aşama)
- AI modülü (ileri aşama)
- Frontend geliştirme (React + TypeScript)

## Verification Checklist

Bir işi bitmiş saymadan ÖNCE:

- [ ] `dotnet build` başarılı
- [ ] `dotnet test` başarılı
- [ ] Authorization kontrolü yapıldı
- [ ] Institution isolation kontrolü yapıldı
- [ ] Input validation eklendi
- [ ] Test coverage yeterli
- [ ] Sensitive data loglanmıyor
- [ ] Code review yapıldı (code-review skill)
- [ ] Security review yapıldı (security-review skill — kritik değişikliklerde)

## Commit Message Convention

```
feat({module}): add {feature}
fix({module}): fix {issue}
refactor({module}): {description}
test({module}): add {test type}
docs: {description}
chore: {description}
security({module}): {description}
```

## Sonraki Adımlar

1. .NET 10 SDK kurulu değil → kullanıcı kuracak
2. `dotnet restore` çalıştırılacak
3. `dotnet build` ile skeleton doğrulanacak
4. Identity modülü implementasyonuna başlanacak
