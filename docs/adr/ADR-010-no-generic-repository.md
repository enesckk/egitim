# ADR-010: No GenericRepository Pattern

## Status
Accepted

## Context
Clean Architecture implementasyonlarında sıkça görülen pattern:
```csharp
public interface IRepository<T>
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

Bu pattern yaygın ancak çoğu durumda gereksiz abstraction'dır.

## Decision
**GenericRepository<T> YASAK.**

### Neden GenericRepository Gereksiz?

1. **EF DbContext zaten Repository**
   - DbSet<T> = collection of entities
   - DbContext.Add/Update/Remove = CRUD operations
   - DbContext.SaveChangesAsync = Unit of Work

2. **Abstraction over Abstraction**
   - EF Core zaten data access abstraction'ı
   - Üzerine bir abstraction daha eklemek gereksiz katman

3. **Query Flexibility Kaybı**
   - Generic repository: `GetAllAsync()`, `FindAsync(predicate)`
   - EF Core LINQ: `Where()`, `Include()`, `Select()`, `OrderBy()`
   - Generic repository LINQ gücünü kısıtlar

4. **Specification Pattern Gerektirir**
   - Complex query'ler için Specification pattern gerekir
   - Bu da ekstra complexity

### Ne Kullanılacak?

**DbContext doğrudan kullanımı:**
```csharp
public class CreateStudentCommandHandler
{
    private readonly ApplicationDbContext _context;

    public async Task Handle(CreateStudentCommand command)
    {
        var student = new Student { ... };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
    }
}
```

**Query Object Pattern (complex query'ler için):**
```csharp
public class GetStudentsWithFilterQuery
{
    public Guid InstitutionId { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class GetStudentsWithFilterQueryHandler
{
    public async Task<IEnumerable<StudentDto>> Handle(GetStudentsWithFilterQuery query)
    {
        return await _context.Students
            .Where(s => s.InstitutionId == query.InstitutionId)
            .Where(s => query.SearchTerm == null || s.Name.Contains(query.SearchTerm))
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new StudentDto { ... })
            .ToListAsync();
    }
}
```

### İstisna: Custom Repository

Eğer bir entity için çok spesifik data access logic varsa:
```csharp
public interface IStudentRepository
{
    Task<Student?> GetStudentWithFullProfileAsync(Guid studentId);
    Task<IEnumerable<Student>> GetTopPerformingStudentsAsync(Guid institutionId, int count);
}

public class StudentRepository : IStudentRepository
{
    // Complex query logic burada
}
```

Bu custom repository, generic değil, entity-specific'tir.

## Consequences

**Pozitif:**
- Daha az kod, daha az abstraction
- EF Core'un tam gücü kullanılabilir
- Query optimization kolay
- Learning curve düşük

**Negatif:**
- Data access logic service layer'da dağılabilir
- Test isolation zor olabilir (mock DbContext vs mock repository)

**Mitigation:**
- Vertical slice pattern ile data access handler içinde kalır
- Integration test ile gerçek DB test edilir
- Unit test için in-memory EF Core provider kullanılabilir

**Katkı:**
- @ardalis - "Generic Repository is an Anti-Pattern"
- @cockrellie - "Repository Pattern with EF Core"
- EF Core documentation: "Repository Pattern is not recommended"
