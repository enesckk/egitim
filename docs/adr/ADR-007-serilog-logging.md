# ADR-007: Serilog Structured Logging

## Status
Accepted

## Context
Logging stratejisi kritik:
- Debugging
- Audit trail
- Security monitoring
- Performance analysis
- Production troubleshooting

Seçenekler:
1. Microsoft.Extensions.Logging (built-in)
2. NLog
3. Serilog
4. log4net

## Decision
**Serilog** benimsendi.

### Neden Serilog?
- **Structured logging** — key-value pair'ler, JSON format
- **Rich sink ecosystem** — MSSQL, Elasticsearch, Seq, Console, File
- **Enrichers** — ThreadId, MachineName, UserId, CorrelationId otomatik eklenir
- **Performance** — async logging, batching
- **.NET native** — ASP.NET Core ile native entegrasyon

### Logging Configuration
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EgitimPlatform.Api")
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.MSSqlServer(
        connectionString: "...",
        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs" })
    .CreateLogger();
```

### Log Seviyeleri
- **Debug:** Development only, detaylı iç durum
- **Information:** İş akışı event'leri (user login, order created)
- **Warning:** Beklenmeyen durum ama işlem devam (retry, fallback)
- **Error:** İşlem başarısız ama uygulama devam
- **Fatal:** Uygulama crash

### Yasak (Sensitive Data)
- Password, token, API key
- PII (TC no, telefon, email — maskeli olabilir)
- Credit card, financial data
- Session ID, cookie value

### İzin Verilen
- UserId, InstitutionId
- Operation adı, endpoint
- Correlation ID
- Error message (sensitive data olmadan)
- Request duration, status code

## Consequences

**Pozitif:**
- Structured log'lar kolayca sorgulanabilir
- MSSQL sink ile merkezi log yönetimi
- Correlation ID ile request tracing
- Sensitive data filtresi otomatik

**Negatif:**
- Log volume yüksek olabilir — rotation/arşiv stratejisi gerekir
- MSSQL sink performans overhead — async batching zorunlu

**Mitigation:**
- Log rotation: günlük dosya, 30 gün saklama
- Async sink ile performance impact minimize
- Log filter ile gereksiz log'lar engellenir
- Production'da Information seviyesi, Debug kapalı
