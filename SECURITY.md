# SECURITY — Egitim Platformu

## Security Mimarisi

Güvenlik sonradan eklenen bir özellik değil, mimarinin temel parçasıdır.

## Authentication

### ASP.NET Core Identity

- Kullanıcı yönetimi: IdentityUser<T> tabanlı
- Şifre: PBKDF2 ile hash (Identity varsayılanı)
- Email confirmation zorunlu
- Two-factor authentication: opsiyonel (ilerleyen aşamada)

### Token Architecture

- **Access token:** JWT, kısa ömürlü (15-30 dakika)
- **Refresh token:** Uzun ömürlü, döndürülebilir, tek kullanımlık
- Access token payload'da: UserId, InstitutionId, Roles
- Token validation: issuer, audience, expiration, signature

### Password Policy

- Minimum uzunluk: 8 karakter
- Karmaşıklık: en az 1 harf, 1 rakam
- Breached password check (HaveIBeenPwned API — ilerleyen aşamada)
- Password reset: email-based token, tek kullanımlık, 1 saat geçerli

## Authorization

### RBAC + Policy + Resource-based

```
1. Rol tabanlı yetkilendirme (Role-based)
   → "InstitutionAdmin", "Coach", "Teacher", "Student", "Parent"

2. Policy tabanlı yetkilendirme
   → "CanViewStudent", "CanEditPlan", "CanAccessReport"

3. Resource-based yetkilendirme (ownership)
   → Coach yalnız assigned student'ların kaynağına erişir
   → Teacher yalnız kendi subject scope'undaki kaynaklara erişir
```

### Institution Isolation

**Zorunlu — hiçbir istisna yoktur.**

- Her authenticated request'te InstitutionId claim'den okunur
- Tüm DB query'lerinde `WHERE InstitutionId = @currentInstitutionId` filtresi
- EF Core Global Query Filter ile otomatik
- Cross-institution erişim denemesi → 403 Forbidden + audit log
- SuperAdmin hariç (tüm institution'lara erişebilir, ancak audit log zorunlu)

### Coach Private Notes

- Coach'un öğrenci için tuttuğu private notes:
  - **Gizli:** InstitutionAdmin, Teacher, Student, Parent
  - **Erişilebilir:** Coach (owner) + SuperAdmin (audit ile)

### Student Anonymous Feedback

- Student'ların Institution/Coach/Teacher hakkında verdiği feedback:
  - **Anonim:** Institution, Coach, Teacher göremez
  - **SuperAdmin:** Identity'yi reveal edebilir
  - **Her reveal** immutable AuditLog oluşturur:
    - RevealedBy (SuperAdmin UserId)
    - RevealedAt (timestamp)
    - FeedbackId
    - Reason (opsiyonel)

## Input Validation

### FluentValidation

- Her API endpoint input'u validate edilir
- Validation hataları → 400 Bad Request + ProblemDetails
- Client-side validation yalnız UX iyileştirmesidir, server-side zorunludur

### XSS Koruması

- Output encoding zorunlu (ASP.NET Core otomatik)
- Input'ta HTML/script tag'leri reddedilir
- Content-Security-Policy header (frontend)

### CSRF Koruması

- Anti-forgery token (form-based request'ler için)
- SameSite cookie attribute
- Custom header validation (API request'ler için)

### SQL Injection

- Parameterized query zorunlu (EF Core otomatik)
- Raw SQL kullanımı yasak (istisna: Backend Lead onayı + review)

### File Upload

- Allowed file types whitelist
- Maximum file size limiti
- Virus scan (ilerleyen aşamada)
- Dosya adı sanitizasyonu
- Secure storage (public erişim yok, authorized download)

## Sensitive Data

### Secrets Management

- Connection string, API key, token → environment variable
- Development: User Secrets (dotnet user-secrets)
- Production: Azure Key Vault / AWS Secrets Manager (deployment'a göre)
- **Source code'a ASLA yazma**

### Logging

- **Yasak:** password, token, API key, PII (TC no, telefon, email — maskeli olabilir)
- **İzin verilen:** UserId, InstitutionId, operation adı, correlation ID
- Structured logging (Serilog) ile key-value pair'ler

### Database

- Hassas alanlar encryption at rest (ilerleyen aşamada)
- PII alanları işaretlenmeli (custom attribute)
- Data retention policy (ilerleyen aşamada)

## Audit Logging

### Kritik İşlemler

Her kritik işlem immutable AuditLog oluşturur:

- User login/logout
- Password change / reset
- Role change
- Feedback identity reveal
- Student enrollment / deletion
- Institution config change

### AuditLog Schema

```
AuditLog {
  Id: Guid
  Timestamp: DateTimeOffset
  UserId: Guid (kim yaptı)
  InstitutionId: Guid
  Action: string (örn: "Feedback.Reveal")
  EntityType: string
  EntityId: string
  OldValues: JSON (opsiyonel)
  NewValues: JSON (opsiyonel)
  IpAddress: string
  UserAgent: string
  CorrelationId: string
}
```

## Rate Limiting

- Per-user rate limiting (token bucket algorithm)
- Per-IP rate limiting (brute force koruması)
- Endpoint-specific limits (login, password reset daha sıkı)
- 429 Too Many Requests response + Retry-After header

## CORS

- Whitelist-based: yalnız bilinen frontend origin'leri
- Development: localhost origins
- Production: spesifik domain'ler

## HTTPS

- Zorunlu (production)
- HSTS header aktif
- TLS 1.2+ (TLS 1.3 tercih)

## Cookie Security

- Secure flag (HTTPS only)
- HttpOnly flag (JS erişimi yok)
- SameSite=Strict (CSRF koruması)
- Short expiration

## Security Headers

- Content-Security-Policy
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy

## Dependency Security

- NuGet paketleri düzenli güncellenmeli
- `dotnet list package --vulnerable` ile kontrol
- Bilinen güvenlik açığı olan paket kullanma
- Lock file (packages.lock.json) ile reproducible build

## OWASP Top 10 Kontrolü

Her önemli değişiklik OWASP Top 10'a göre gözden geçirilir:

1. Broken Access Control
2. Cryptographic Failures
3. Injection
4. Insecure Design
5. Security Misconfiguration
6. Vulnerable and Outdated Components
7. Identification and Authentication Failures
8. Software and Data Integrity Failures
9. Security Logging and Monitoring Failures
10. Server-Side Request Forgery

## Security Review Süreci

1. Yeni endpoint → authorization kontrolü
2. Yeni data erişimi → institution isolation kontrolü
3. Sensitive data değişikliği → audit logging kontrolü
4. External entegrasyon → input validation + output encoding
5. Production deployment → security checklist + penetration test (yıllık)
