# Agentic Coding Instructions for Maliev.PredictionService

This document contains instructions for AI agents operating in this repository.

## 1. Service Scope

**IMPORTANT:** The ML.NET regression model for **print time / price prediction** is **permanently retired**.
This service remains active for other prediction use cases:

- **Business Forecasting** — demand forecasting, seasonal trends
- **Pricing Analytics** — historical pricing analysis, market trends
- **Operational Predictions** — queue depth predictions, lead time estimates
- **General ML/AI** — future prediction capabilities as business needs evolve

## 2. Environment & Build

- **Framework**: .NET 10.0 (C# 13)
- **Database**: PostgreSQL 18 (using Entity Framework Core 10)
- **Architecture**: Clean Architecture (Api, Application, Domain, Infrastructure, Tests)
- **TreatWarningsAsErrors**: ENABLED. Zero compilation warnings allowed.

### Commands

- **Build**: `dotnet build`
- **Test (All)**: `dotnet test`
- **Test (Single)**: `dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
- **Run API**: `dotnet run --project Maliev.PredictionService.Api`
- **Database Migrations**: `dotnet ef migrations add <MigrationName> --project Maliev.PredictionService.Infrastructure --startup-project Maliev.PredictionService.Infrastructure`
- **Database Update**: `dotnet ef database update --project Maliev.PredictionService.Infrastructure --startup-project Maliev.PredictionService.Infrastructure`

## 3. Code Style & Conventions

### General
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Maliev.PredictionService.Domain.Entities;`).
- **Formatting**: Standard C# conventions (PascalCase for classes/methods, camelCase for local variables).
- **Nullability**: `Nullable` context is ENABLED. Handle nulls explicitly. Use `?` for optional references.
- **Documentation**: XML documentation `///` is **REQUIRED** for all public methods and properties.

### Domain Entities
- **IDs**: Use `Guid` for primary keys.
- **Dates**: Use `DateTimeOffset` instead of `DateTime`.
- **Collections**: Initialize collection properties (e.g., `public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();`).
- **Navigation Properties**: Mark as nullable if optional.

### Architecture Rules (Strict)
- **No AutoMapper**: Perform manual mapping.
- **No FluentValidation**: Use Data Annotations (`[Required]`, `[EmailAddress]`).
- **No FluentAssertions**: Use standard xUnit `Assert`.
- **No In-Memory DB**: Use **Testcontainers** for integration tests.
- **No Secrets**: Configuration via environment variables only.

## 4. Testing Guidelines

- **Integration over Unit**: Prioritize integration tests using Testcontainers/PostgreSQL.
- **Naming**: `MethodName_StateUnderWhichTestIsRunning_ExpectedBehavior` (e.g., `PredictDemand_WithValidData_ReturnsForecast`).
- **Structure**: Arrange, Act, Assert comments are optional but encouraged for complex tests.

## 5. Specific Workflows

### Adding a New Prediction Model
1. Define Entity in `Domain` (e.g., `Prediction`, `TrainingData`, `ModelVersion`).
2. Create Repository Interface in `Domain/Interfaces`.
3. Implement Repository in `Infrastructure`.
4. Create Prediction Service/Handler in `Application`.
5. Create Controller/Endpoint in `Api`.
6. Add Integration Tests in `Tests`.

### Modifying Database
1. Modify Entity in `Domain`.
2. Create Migration (`dotnet ef migrations add ...`).
3. Update `DbContext` in `Infrastructure` if necessary.

## 6. Agent Behavior
- **Proactive Fixes**: If you see a warning, fix it.
- **Verification**: ALWAYS run `dotnet build` after changes.
- **Safety**: Do not commit secrets.


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
