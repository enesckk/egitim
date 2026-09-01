# ADR-003: MSSQL + EF Core

## Status
Accepted

## Context
Veritabanı seçimi kritik bir mimari karardır. Gereksinimler:
- Kurumsal destek
- Transactional integrity
- Multi-tenant deployment desteği
- .NET ekosistemi ile native entegrasyon
- Performance at scale

Seçenekler: PostgreSQL, MySQL, SQL Server (MSSQL), MongoDB

## Decision
**Microsoft SQL Server (MSSQL)** + **Entity Framework Core** benimsendi.

**MSSQL tercih sebepleri:**
- .NET ekosistemi ile native entegrasyon (Azure SQL, LocalDB)
- Enterprise feature'lar: Always Encrypted, Row-Level Security, Temporal Tables
- Kurumsal destek ve SLA
- Türkiye'de yaygın kullanım, ekip aşinalığı

**EF Core tercih sebepleri:**
- .NET'in resmi ORM'i
- Code-First migration desteği
- LINQ ile type-safe query
- Global Query Filter (institution isolation için kritik)
- Change tracking, optimistic concurrency desteği

## Consequences

**Pozitif:**
- Güçlü transaction desteği
- Enterprise feature'lar erişilebilir
- EF Core Global Query Filter ile institution isolation otomatik
- Migration yönetimi standart

**Negatif:**
- License maliyeti (SQL Server Standard/Enterprise)
- Linux deployment'ta Docker gereksinimi
- EF Core learning curve

**Mitigation:**
- Development: LocalDB veya Docker SQL Server (ücretsiz)
- Production: Azure SQL veya SQL Server on-premise
- EF Core eğitim dokümantasyonu sağlanır

**Alternatif değerlendirme:**
- PostgreSQL: Ücretsiz, güçlü, ancak EF Core provider'ı MSSQL kadar mature değil
- MySQL: Yaygın, ancak enterprise feature'lar zayıf
- MongoDB: Document model uygun değil (relational data ağırlıklı)
