---
description: Build a new Materia feature end-to-end using the CLAUDE.md TDD checklist (Domain → tests → Application → Infrastructure → endpoint → green), then security review.
argument-hint: <feature description>
---

Implement this feature end-to-end for Materia, following the project's "When Adding a New Feature" workflow exactly. Honor Clean Architecture (Domain → Application → Infrastructure, dependencies inward only), Event Sourcing (every state change via a past-tense domain event; append-only; replay for state), and TDD (Red → Green → Refactor).

**Feature:** $ARGUMENTS

Work in this order, delegating to the `backend-engineer` subagent for the implementation:

1. **Domain first** — define the aggregate, value objects, and past-tense domain events.
2. **Write xUnit tests** for the expected behavior BEFORE implementing (confirm they fail = Red).
3. **Application layer** — implement the command/query handler + its FluentValidation validator (validator tested separately).
4. **Infrastructure** — implement the repository / event store. Add an EF Core migration if schema changes (`dotnet ef migrations add <Name> --project Materia.Infrastructure --startup-project Materia.ApiService`) — never hand-edit schema.
5. **ApiService** — expose the endpoint and wire DI.
6. **Green gate** — run `dotnet build` and `dotnet test`; all tests must pass. Refactor if needed, keeping tests green.

Then:
7. If the feature touches **money, credit/debt, cash, customer PII, or auth**, hand the diff to the `security-engineer` subagent for review and address Critical/High findings before reporting done.

Finish with a short summary: aggregate + events added, tests written and their status, any migration, endpoint(s) exposed, and security verdict. If any requirement in the description is ambiguous (e.g. authorization rules, rounding, who can perform the action), ask before implementing rather than guessing.
