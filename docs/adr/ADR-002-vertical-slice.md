# ADR-002: Vertical Slice Architecture

## Status
Accepted

## Context
Geleneksel katmanlı mimari (Controller → Service → Repository) şu sorunları yaratır:
- Bir özellik için 5-6 dosya arasında gezmek gerekir
- Cross-cutting concern'leri yönetmek zor
- Feature bazlı düşünmek zor, layer bazlı düşünmek kolay
- Code organization feature'a göre değil, technical concern'e göre

## Decision
**Vertical Slice Architecture** benimsendi.

Her özellik (feature) kendi dikey dilimini oluşturur:

```
Features/
  CreateStudent/
    CreateStudentCommand.cs
    CreateStudentCommandHandler.cs
    CreateStudentValidator.cs
  GetStudent/
    GetStudentQuery.cs
    GetStudentQueryHandler.cs
```

Her slice: endpoint → validation → business logic → data access içerir.

Light CQRS pattern uygulanır: Command/Query ayrımı, MediatR zorunlu değil.

## Consequences

**Pozitif:**
- Feature bazlı organizasyon — bir özelliğin tüm kodu bir arada
- Cross-cutting concern'ler (validation, logging, auth) pipeline behavior ile eklenir
- Code navigation kolay
- Feature deletion temiz (bir klasör sil)

**Negatif:**
- Code duplication riski (her slice kendi handler'ını yazar)
- Shared logic için abstraction gerektiğinde ek klasör gerekir

**Mitigation:**
- Kod tekrarı fark edildiğinde BuildingBlocks'a çekilir
- Premature abstraction yapılmaz, 3+ tekrar sonra refactor düşünülür
