# MASTER PRODUCT SPECIFICATION — Egitim Platformu

## Ürün Tanımı

Egitim Platformu, eğitim kurumları (kurs, dershane, okul) için tasarlanmış ticari bir **EdTech SaaS/PWA** ürünüdür.

Kurumlar platformu bağımsız deployment'lar olarak kullanır. Her kurum kendi veritabanı, konfigürasyonu ve depolama alanına sahiptir. Ancak uygulama katmanında **institution-level isolation** ve **cross-institution erişim engeli** zorunludur.

## Hedef Kullanıcılar

| Rol | Açıklama |
|-----|----------|
| **SuperAdmin** | Platform yöneticisi. Tüm sistem üzerinde tam yetki. Audit log'a erişim. Feedback identity'yi reveal edebilir. |
| **InstitutionAdmin** | Kurum yöneticisi. Yalnız kendi kurumu üzerinde tam yetki. Coach private notes'a erişemez. |
| **Coach** | Öğrenci koçu. Yalnız atanmış öğrencileri görür. Private notes tutabilir (InstitutionAdmin dahil diğer rollerden gizli). |
| **Teacher** | Öğretmen. Yalnız atanmış öğrencileri/grupları ve kendi ders alanını görür. |
| **Student** | Öğrenci. Platformun ana kullanıcısı. Sınav, ders, plan, defter, rapor özelliklerini kullanır. |
| **Parent** | Veli. Yalnız ilişkilendirildiği öğrencinin sadeleştirilmiş bilgilerini görür. |

## Temel Yetki Kuralları

1. **RBAC + Policy + Resource-based/Ownership authorization** kullanılacak.
2. **Institution isolation** — hiçbir endpoint cross-institution erişime izin vermez.
3. **Coach** yalnız assigned/authorized students görür.
4. **Teacher** yalnız assigned students/groups ve kendi subject scope'unu görür.
5. **Parent** yalnız ilişkilendirildiği öğrencinin sadeleştirilmiş bilgilerini görür.
6. **Coach private notes** — InstitutionAdmin, Teacher, Student, Parent'tan gizli.
7. **Student anonymous feedback** — Institution, Coach, Teacher'a anonim.
8. **Hidden feedback identity** — yalnız SuperAdmin tarafından reveal edilebilir. Her reveal **immutable AuditLog** oluşturur.
9. **Cross-institution erişim** hiçbir endpoint'te mümkün değildir.

## Backend Modülleri

| Modül | Sorumluluk |
|-------|-----------|
| **Identity** | Kullanıcı, rol, izin, authentication, token yönetimi |
| **Institutions** | Kurum, şube, sınıf, grup yönetimi |
| **Students** | Öğrenci profilleri, atamalar, veli ilişkileri |
| **Coaching** | Koçluk atamaları, koç notları, koç-öğrenci ilişkileri |
| **Academic** | Ders, konu, alt konu, öğrenme kazanımları, kavram haritası |
| **Questions** | Soru bankası (ileri aşamada) |
| **Exams** | Sınav türleri, sınav yönetimi (ileri aşamada) |
| **Learning** | Öğrenme kayıtları, ilerleme takibi |
| **Planning** | Çalışma planı, görev, hedef |
| **Content** | Ders materyalleri, dosya yönetimi |
| **Communication** | Bildirimler, mesajlaşma |
| **Notebook** | Öğrenci defteri |
| **Reporting** | Raporlama, analiz |
| **AI** | AI entegrasyonu (ileri aşamada) |

## Domain Temel Modelleri

### Identity
- User, Role, Permission, UserRole, RolePermission

### Institution
- Institution, Branch, Classroom, Group

### People
- Student, Coach, Teacher, Parent
- StudentCoachAssignment, StudentTeacherAssignment, TeacherSubject, StudentParent

### Academic
- ExamType, Subject, Topic, SubTopic, LearningOutcome, Concept

### Planning
- StudyPlan, StudyPlanItem, Task, Goal

### System
- AuditLog, Notification, FileAsset

> **Not:** Student, User'dan ayrı bir domain profile'd. User authentication entity'sidir, Student akademik profil entity'sidir.

## API Konvansiyonları

- `/api/v1` prefix
- DTO only response — EF entity doğrudan response olarak YASAK
- FluentValidation ile input validation
- ProblemDetails ile hata response'ları
- Pagination, filter, sorting desteği
- OpenAPI / Swagger dokümantasyonu

## Database Konvansiyonları

- **MSSQL** (Microsoft SQL Server)
- Migration'lar yalnız Backend Lead tarafından yönetilir
- Critical deletes — çoğunlukla soft-delete
- Optimistic concurrency gereken yerlerde RowVersion
- Institution isolation için index ve query pattern optimizasyonu

## Security Gereksinimleri

- ASP.NET Core Identity
- Secure access/refresh token architecture
- Password reset ve verification infrastructure
- Resource-based authorization
- Secure file access
- Input validation (FluentValidation)
- No secrets in source code
- No sensitive data in logs
- Correlation IDs ile request tracing
- Structured logging (Serilog)
- Critical action audit logging
- Rate-limit-ready design

## Henüz Implement Edilmeyecek (İleri Aşama)

- Exam detaylı implementasyon
- Question Bank detaylı implementasyon
- Knowledge Map detaylı implementasyon
- Local AI implementasyonu
- PDF Analysis
- OMR (Optical Mark Recognition)
- WebRTC
- WhatsApp entegrasyonu

## Deployment Modeli

- **Tenant-per-instance / Isolated deployment**
- Her kurum ayrı database, config, storage kullanabilir
- Application katmanında InstitutionId/context authorization zorunlu
