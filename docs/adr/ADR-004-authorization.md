# ADR-004: RBAC + Policy + Resource-based Authorization

## Status
Accepted

## Context
Egitim Platformu karmaşık yetkilendirme gereksinimlerine sahip:
- Rol tabanlı erişim (InstitutionAdmin, Coach, Teacher, Student, Parent)
- Institution isolation (cross-institution erişim yasak)
- Ownership-based erişim (Coach yalnız assigned students görür)
- Resource-based erişim (özel notlar, anonim feedback)

Tek başına RBAC yetersiz:
- "InstitutionAdmin" rolü her kuruma erişemez, yalnız kendi kurumuna
- "Coach" rolü her öğrenciyi göremez, yalnız atanmış öğrencileri
- Coach private notes InstitutionAdmin'den bile gizli

## Decision
**Katmanlı yetkilendirme modeli** benimsendi:

### 1. RBAC (Role-Based Access Control)
Temel rol atamaları:
- SuperAdmin, InstitutionAdmin, Coach, Teacher, Student, Parent

### 2. Policy-Based Authorization
Rol üstü kurallar:
- `CanViewStudent` policy
- `CanEditPlan` policy
- `CanAccessReport` policy
- Policy'ler rol + institution scope + diğer koşulları kontrol eder

### 3. Resource-Based Authorization
Ownership kontrolü:
- Coach → yalnız assigned student'ların kaynağına erişir
- Teacher → yalnız kendi subject scope'undaki kaynaklara erişir
- Parent → yalnız ilişkilendirildiği öğrencinin bilgilerine erişir

### 4. Institution Isolation (Mandatory)
- Her authenticated request'te InstitutionId claim'den okunur
- EF Core Global Query Filter ile tüm query'lerde otomatik filtre
- Cross-institution erişim → 403 Forbidden + audit log

## Consequences

**Pozitif:**
- Esnek ve granular yetkilendirme
- Institution isolation otomatik ve güvenli
- Ownership kontrolü iş kurallarını doğru yansıtır
- Audit trail ile yetki ihlalleri izlenebilir

**Negatif:**
- Karmaşık authorization logic
- Her endpoint'te explicit kontrol gerekir
- Test coverage kritik

**Mitigation:**
- Authorization handler'lar merkezi ve test edilebilir
- Integration test ile institution isolation doğrulanır
- Security review her endpoint değişikliğinde yapılır
