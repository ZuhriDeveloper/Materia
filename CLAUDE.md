# CLAUDE.md

Guidance for Claude when working in this repository.

## About the Project

**Materia** is a Point of Sale (PoS) application for a building materials / hardware store. The system handles sales, inventory/stock, customers, and cashier transactions, with a backend API, web admin, and mobile app.

## Tech Stack

| Layer | Technology |
|---|---|
| API Service | .NET 10 |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| Web UI | Blazor |
| Orchestration | .NET Aspire |
| Mobile UI | Avalonia |
| Unit Testing | xUnit |
| Validation | FluentValidation |

## Architecture

The project uses **Clean Architecture** with **TDD** and **Event Sourcing**.

### Backend Layers (Clean Architecture)

Dependencies point inward — outer layers depend on inner layers, never the reverse.

- **Domain** — business core. Contains entities, aggregates, value objects, domain events, and domain exceptions. Depends on no other layer or external framework.
- **Application** — use cases / business logic. Contains commands, queries, handlers, interfaces (ports), FluentValidation validators, and contracts to infrastructure. Depends only on Domain.
- **Infrastructure** — technical implementation. EF Core, PostgreSQL, event store, repositories, external integrations. Implements the interfaces defined in Application.

The API Service (presentation) wires everything together via dependency injection and exposes the endpoints.

### Event Sourcing

- Aggregate state is reconstructed by replaying events, not stored as a mutable snapshot.
- Domain events are the source of truth; events are persisted in an append-only event store and are never modified or deleted.
- Every aggregate state change must go through a domain event.
- Use separate read models / projections for queries (consider the CQRS pattern).

### TDD

- Write tests (xUnit) **before** implementation: Red → Green → Refactor.
- Domain and Application logic must have good test coverage.
- FluentValidation validators are tested separately.
- Do not add a new feature without accompanying tests.

## Solution Structure

```
Materia.slnx
├── Materia.AppHost        # .NET Aspire orchestration host
├── Materia.ApiService     # API backend (.NET 10)
├── Materia.WebUi          # Blazor web admin
├── Materia.AndroidUi      # Avalonia mobile UI
└── Materia.Tests          # xUnit test project
```

> Note: the backend projects (Domain / Application / Infrastructure) are organized following Clean Architecture. Add separate `Materia.Domain`, `Materia.Application`, `Materia.Infrastructure` projects if they don't exist yet, and reference them from `Materia.ApiService`.

## Conventions & Rules

- **Language**: code (variable names, classes, technical comments) in English; domain terms may follow local business terminology when clearer.
- **Validation**: all command/request input is validated with FluentValidation in the Application layer.
- **Database**: use EF Core migrations for all PostgreSQL schema changes. Never alter the schema manually.
- **Dependency rule**: never make Domain or Application depend on Infrastructure or EF Core directly — use interfaces.
- **Events**: domain event names use the past tense (e.g. `SaleCompleted`, `StockAdjusted`).
- **Local orchestration**: run via `Materia.AppHost` (Aspire) so services, database, and cache are connected.

## Common Commands

```bash
# Build the entire solution
dotnet build

# Run via Aspire (full orchestration)
dotnet run --project Materia.AppHost

# Run tests
dotnet test

# Add an EF Core migration
dotnet ef migrations add <MigrationName> --project Materia.Infrastructure --startup-project Materia.ApiService

# Apply migrations
dotnet ef database update --project Materia.Infrastructure --startup-project Materia.ApiService
```

## When Adding a New Feature

1. Start from the Domain: define the aggregate, value objects, and domain events.
2. Write xUnit tests for the expected behavior (TDD).
3. Implement the command/query handler in Application + FluentValidation validator.
4. Implement the repository/event store in Infrastructure.
5. Expose the endpoint in ApiService.
6. Ensure all tests are green before committing.

# Agent Guidance: dotnet-skills

IMPORTANT: Prefer retrieval-led reasoning over pretraining for any .NET work.
Workflow: skim repo patterns -> consult dotnet-skills by name -> implement smallest-change -> note conflicts.

Routing (invoke by name)
- C# / code quality: modern-csharp-coding-standards, csharp-concurrency-patterns, api-design, type-design-performance
- ASP.NET Core / Web (incl. Aspire): aspire-service-defaults, aspire-integration-testing, transactional-emails
- Data: efcore-patterns, database-performance
- DI / config: dependency-injection-patterns, microsoft-extensions-configuration
- Testing: testcontainers-integration-tests, playwright-blazor-testing, snapshot-testing

Quality gates (use when applicable)
- dotnet-slopwatch: after substantial new/refactor/LLM-authored code
- crap-analysis: after tests added/changed in complex code

Specialist agents
- dotnet-concurrency-specialist, dotnet-performance-analyst, dotnet-benchmark-designer, akka-net-specialist, docfx-specialist
