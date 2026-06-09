---
description: Pre-commit gate for Materia — build, run all tests, format, and self-review the current diff against project conventions.
---

Run the pre-commit quality gate for Materia and report results concisely. Do NOT commit anything — just verify and report.

1. **Build** — run `dotnet build`. Report any errors/warnings.
2. **Test** — run `dotnet test`. All tests must pass; if any fail, show the failing test names and the assertion messages.
3. **Format** — run `dotnet format --verify-no-changes` (or `dotnet format` and report what it changed) so style is consistent.
4. **Diff self-review** — run `git diff` (and `git status`) and check the staged/unstaged changes against the project's rules:
   - Clean Architecture dependency rule respected (no Domain/Application reference to Infrastructure or EF Core).
   - Every aggregate state change goes through a past-tense domain event; no event mutation/deletion.
   - Command/request input validated with FluentValidation.
   - New schema changes are via EF Core migration, not manual edits.
   - New backend behavior has accompanying xUnit tests.
   - Money uses `decimal`; no secrets/connection strings added to source or appsettings.

Report a clear verdict: **READY TO COMMIT** or **NEEDS WORK**, with a bulleted list of anything that must be fixed first. If the diff touches money, credit/debt, cash, or customer PII, recommend running the `security-engineer` review before committing.
