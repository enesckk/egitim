# ADR-009: Soft-Delete Strategy

## Status
Accepted

## Context
Data deletion stratejisi kritik:
- Accidental deletion recovery
- Audit trail (kim, ne zaman sildi)
- Referential integrity (ilişkili kayıtlar)
- Compliance (veri saklama zorunluluğu)
- Performance (büyük tablolar)

Seçenekler:
1. Hard delete — fiziksel silme
2. Soft delete — IsDeleted flag
3. Hybrid — kritik kayıtlar soft, geçici kayıtlar hard

## Decision
**Soft-delete varsayılan, hard-delete istisna** benimsendi.

### Soft-Delete Implementation
```csharp
public abstract class BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

### EF Core Global Query Filter
```csharp
modelBuilder.Entity<Student>().HasQueryFilter(s => !s.IsDeleted);
```

Tüm query'ler otomatik olarak `IsDeleted = false` filtresi uygular.

### Hard-Delete İstisnalar
Soft-delete uygun OLMAYAN durumlar:
- Temp data (session, cache)
- Log entries (immutable)
- Audit entries (immutable)
- Temporary tokens (expired)
- Bulk import staging data

### Restore Capability
Soft-deleted record'lar geri getirilebilir:
```csharp
context.Students.IgnoreQueryFilters()
    .Where(s => s.Id == id)
    .ExecuteUpdate(s => s.SetProperty(s => s.IsDeleted, false));
```

### Cascade Behavior
Parent soft-delete → children de soft-delete olmalı:
- Institution deleted → Branch, Classroom, Group soft-delete
- Student deleted → Enrollment, Plan soft-delete
- Trigger veya application logic ile cascade

## Consequences

**Pozitif:**
- Accidental deletion recovery
- Audit trail (kim, ne zaman sildi)
- Referential integrity korunur
- Compliance gereksinimleri karşılanır

**Negatif:**
- Database boyutu büyür (silinen kayıtlar kalır)
- Query performance impact (filter overhead)
- Unique constraint komplikasyonu (silinen + aktif kayıt aynı unique key)
- Cascade logic karmaşık

**Mitigation:**
- Index on IsDeleted — filter performance optimize
- Archive strategy — eski soft-deleted kayıtlar archive table'a taşınır
- Unique constraint: `WHERE IsDeleted = false` partial index
- Cleanup job — 1 yıldan eski soft-deleted kayıtlar hard-delete (archived sonra)

**Performance:**
- Index: `CREATE INDEX IX_Student_IsDeleted ON Students(IsDeleted) WHERE IsDeleted = 0`
- Partitioning: büyük tablolar için IsDeleted bazlı partition
- Archive job: aylık, 1 yıldan eski soft-deleted kayıtlar archive table'a
