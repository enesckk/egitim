# ADR-011: Neutral Persistence Layer

## Status

Accepted (2026-09-03)

## Context

ApplicationDbContext and EF Core migrations were initially placed in the Identity module because Identity was the first module and the natural home for user-related persistence. As domain modules (Students, Coaching) grew, they needed direct access to `ApplicationDbContext` for querying their own entities. This created an undesirable dependency direction:

- `Students` module → `Identity` module (concrete `ApplicationDbContext`)
- `Coaching` module → `Identity` module + `Students` module

The Identity module was becoming a "God module" — owning not just identity/auth but also platform-level persistence composition. This violates modular monolith principles: domain modules should depend on abstractions, not on another module's concrete infrastructure.

## Decision

Create `EgitimPlatform.Infrastructure` as a neutral persistence layer:

1. **ApplicationDbContext** moved from `Modules.Identity.Infrastructure` to `Infrastructure`.
2. **EF Core migrations** moved alongside the DbContext.
3. **Infrastructure project** owns DbContext registration (`AddPlatformInfrastructure`), Identity EF stores wiring, and cross-module FK configuration.
4. **Domain modules** access persistence through `IApplicationDbContext` (defined in `BuildingBlocks`), never through the concrete `ApplicationDbContext`.
5. **Identity module** retains entity definitions (ApplicationUser, ApplicationRole, etc.), configurations, auth services, and controllers — but no longer owns the shared DbContext.

### Dependency Direction

```
Infrastructure → Identity (entity types for IdentityDbContext<,,>)
              → BuildingBlocks (interfaces, base entities)

Identity → BuildingBlocks (IApplicationDbContext, interfaces)

Students → BuildingBlocks (IApplicationDbContext, ICurrentUser, etc.)

Coaching → BuildingBlocks + Students (entity types for Student)

Api → Infrastructure + all modules (composition root)
```

No circular dependencies. Domain modules never reference Infrastructure directly.

### Module EF Configuration Discovery

Module entity configurations are discovered at runtime via `Assembly.Load`:

```csharp
var moduleNames = new[] { "EgitimPlatform.Modules.Students", ... };
foreach (var name in moduleNames)
{
    var assembly = Assembly.Load(name);
    builder.ApplyConfigurationsFromAssembly(assembly);
}
```

**Tradeoff**: No compile-time safety for configuration discovery — a missing assembly silently skips. This is acceptable because:
- Modules are registered at the composition root (Api project), ensuring assemblies are in the output.
- Missing module configurations would be caught immediately by integration tests.
- Design-time migration tools resolve assemblies via the startup project.

### What Infrastructure Can Contain

- `ApplicationDbContext` and migrations
- Cross-module FK configuration (reflection-based)
- `InfrastructureServiceExtensions` (DI registration)
- Platform-level concerns that don't belong to any domain module

### What Infrastructure Cannot Contain

- Domain logic or business rules
- Module-specific repositories or services
- Generic repository abstractions (see ADR-010)
- Module entity definitions (those stay in their modules)

## Why Not Generic Repository / UnitOfWork

See ADR-010. Domain modules use `IApplicationDbContext.Set<T>()` directly. Adding a generic repository layer would create unnecessary abstraction without providing real decoupling — modules still need EF Core-specific features (query filters, eager loading, projections).

## Consequences

### Positive
- Clear separation: domain modules depend on BuildingBlocks abstractions, not Identity concrete types
- Identity module is no longer a "God module" — it owns identity, not platform persistence
- New domain modules can access persistence without referencing Identity
- Migration ownership is explicit and centralized
- Follows the dependency inversion principle

### Negative
- Runtime assembly discovery for module configurations (mitigated by integration tests)
- Additional project in the solution (minimal overhead)
- Migration generation requires specifying both startup and Infrastructure projects

### Future Split Conditions

If the platform grows to require:
- **Per-module databases**: Each module gets its own DbContext in its own project. Infrastructure becomes a thin composition layer.
- **CQRS read models**: Read-side DbContexts can live in Infrastructure or in dedicated read-model projects.
- **Event-driven module communication**: Infrastructure could host a message bus abstraction, but domain events should remain in domain modules.

## Related

- ADR-001 (Modular Monolith)
- ADR-003 (MSSQL + EF Core)
- ADR-010 (No Generic Repository)
