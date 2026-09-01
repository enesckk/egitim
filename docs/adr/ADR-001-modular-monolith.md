# ADR-001: Modular Monolith Architecture

## Status
Accepted

## Context
Egitim Platformu ticari bir EdTech SaaS ürünüdür. Erken aşamada microservice mimarisine geçmek operational overhead yaratır:
- Birden fazla servis deploy, monitor, scale etmek gerekir
- Distributed transaction yönetimi gerekir
- Tek ekip için coordination maliyeti yüksektir

Ancak monolith'in geleneksel sorunları (tight coupling, deploy bağımlılığı) de vardır.

## Decision
**Modular Monolith** benimsendi.

Tek deployment birimi, ancak modüler iç yapı:
- Her modül (Identity, Institutions, Students, vb.) kendi içinde izole
- Modüller arası iletişim contract/event üzerinden
- Her modülün kendi DI registration'ı, kendi middleware'i olabilir
- Modüller birbirini doğrudan referans almaz

Gelecekte microservice'e geçiş gerekirse, modüller servis sınırına dönüştürülebilir.

## Consequences

**Pozitif:**
- Tek deploy, tek CI/CD pipeline
- Basit debugging, tracing
- Modüler yapı sayesinde gelecekte microservice'e geçiş mümkün
- Tek ekip için uygun

**Negatif:**
- Modül sınırlarını korumak disiplin gerektirir
- Deployment birimi büyüdükçe build süresi artar
- Scale etmek için tüm uygulamayı scale etmek gerekir

**Risk mitigasyonu:**
- Code review ile modül sınırları korunur
- Build parallelization ile süre optimize edilir
- Modüller arası bağımlılık tool ile izlenir
