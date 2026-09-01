# ADR-005: Tenant-per-Instance Deployment

## Status
Accepted

## Context
Egitim Platformu commercial multi-tenant bir üründür. Multi-tenancy modelleri:

1. **Shared database, shared schema** — tüm tenant'lar aynı DB, InstitutionId ile ayrım
2. **Shared database, separate schema** — her tenant'ın kendi schema'sı
3. **Separate database** — her tenant'ın kendi DB'si
4. **Separate instance** — her tenant'ın kendi deployment'ı

## Decision
**Tenant-per-instance / Isolated deployment** benimsendi.

Her kurum (tenant) için:
- Ayrı database
- Ayrı configuration
- Ayrı file storage
- Ayrı deployment instance

Ancak application katmanında **InstitutionId-based authorization** yine de zorunludur:
- SuperAdmin rolü birden fazla institution'a erişebilir
- Audit logging tüm institution'lar için merkezi
- Platform-level feature'lar (billing, support) institution-agnostic

## Consequences

**Pozitif:**
- Tam veri izolasyonu — kurum verileri kesinlikle ayrı
- Performance izolasyonu — bir kurumun yükü diğerini etkilemez
- Custom deployment — kurum özel konfigürasyon mümkün
- Compliance — veri egemenliği gereksinimleri karşılanır

**Negatif:**
- Operational overhead — her kurum için ayrı instance
- Deployment complexity — CI/CD pipeline çoğalır
- Cost — her kurum için ayrı infrastructure
- Update coordination — tüm instance'lar güncel tutulmalı

**Mitigation:**
- Infrastructure-as-Code (Terraform, ARM templates)
- Merkezi CI/CD pipeline, otomatik deployment
- Monitoring ve alerting merkezi
- Version management stratejisi

**Gelecek:**
- İlk aşamada manual deployment
- Ölçek büyüdükçe Kubernetes orchestration değerlendirilir
