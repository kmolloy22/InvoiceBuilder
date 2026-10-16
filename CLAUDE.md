# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

InvoiceBuilder — a full-stack .NET app for creating, managing, and issuing invoices. Users maintain customers and sender profiles, assemble invoices from line items, and (eventually) export them as PDF. See [plans/PLAN.md](plans/PLAN.md) for current status, milestones, and open architectural questions (multi-tenancy, tax handling, PDF approach, invoice numbering) — read it before starting non-trivial work, and update it when scope/architecture changes.

## Commands

Solution file is `InvoiceBuilder.slnx` at the repo root (not under `backend/`).

```bash
# Restore / build / test everything (matches CI)
dotnet restore InvoiceBuilder.slnx
dotnet build InvoiceBuilder.slnx --configuration Release
dotnet test InvoiceBuilder.slnx --configuration Release

# Build/test a single project
dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release
dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj -c Release

# Run a single test
dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests -c Release --filter "FullyQualifiedName~CustomersEndpointsTests.Post_creates_customer_returns_201_and_location"

# Run the full app (Aspire orchestrates Postgres + pgAdmin + API + Web) — requires Docker Desktop running
dotnet run --project backend/src/InvoiceBuilder.AppHost
```

Integration tests use Testcontainers (`Testcontainers.PostgreSql`) and require Docker to be running locally.

CI (`.github/workflows/ci.yml`) runs `dotnet restore/build/test` against the root `InvoiceBuilder.slnx` only (backend + tests); there is no separate frontend CI job (it exists commented-out in the workflow).

## Architecture

.NET Aspire solution with a vertical-slice backend and a Blazor Server frontend.

- **`InvoiceBuilder.AppHost`** — Aspire orchestrator ([AppHost.cs](backend/src/InvoiceBuilder.AppHost/AppHost.cs)): provisions Postgres + pgAdmin, wires the API and Web projects together via service discovery. This is the entry point for local `dotnet run`.
- **`InvoiceBuilder.Domain`** — entities (`Customer`, `Sender`, `Invoice`, `InvoiceLineItem`). Entities are `sealed`, expose no public constructor, and are mutated only through a static `Create(...)` factory and an instance `Update(...)` method (private setters otherwise). Cross-cutting `Result<T>`/`ResultError` types live in `Results/` and are the return type for all Application handlers — `IsFailure`/`Error` on failure, never exceptions for expected validation/not-found cases.
- **`InvoiceBuilder.Application`** — vertical-slice features under `Features/{Customers,Senders,Invoices}/`, each with a MediatR `IRequest`/`IRequestHandler` pair per operation (e.g. `CreateCustomer.Handler.cs` defines both `CreateCustomerCommand` and the internal handler in one file), a `Models/{Create,Update,Get}/*Dto.cs` folder for request DTOs, a `Validation/*Validator.cs` FluentValidation validator per DTO, and shared `Result` records under `Shared/Responses/{Feature}/`. Handlers talk to `InvoiceBuilderContext` (EF Core) directly — no repository layer.
- **`InvoiceBuilder.Api`** — minimal-API endpoints, one static class per operation under `Features/{Feature}/{Operation}/*.Endpoint.cs`, registered onto a `MapGroup` in a per-feature `{Feature}Endpoint.cs` (e.g. `CustomerEndpoint.MapCustomers`), all wired up in `Program.cs`. Every mutating endpoint applies `.AddEndpointFilter<ValidationFilter<TDto>>()`, which runs the matching FluentValidation validator and returns `Results.ValidationProblem(...)` (400) grouped by `PropertyName` — the API layer never re-validates manually. Unhandled exceptions are caught by `GlobalExceptionHandler` and turned into an RFC 7807 Problem response with a `TraceId`. List endpoints use cursor-based pagination (`InvoiceBuilder.Application.Shared.Pagination.CursorPagination`/`CursorPage<T>`) — not offset/skip paging.
- **`InvoiceBuilder.Database`** — EF Core + Npgsql, `InvoiceBuilderContext`, `Configurations/*EntityConfiguration.cs` (one per entity), and `Migrations/`. `DataExtensions.InitializeDbAsync` (called from `Program.cs` before `RunAsync`) runs `Database.MigrateAsync()` then seeds demo Customers/Senders/Invoices if none exist — this also means `WebApplicationFactory`-based integration tests get a migrated + seeded DB for free with no explicit setup.
- **`InvoiceBuilder.Shared.Kernel`** — cross-cutting primitives shared across layers.
- **`InvoiceBuilder.Web`** (frontend, `frontend/src/InvoiceBuilder.Web`) — Blazor Server (interactive server render mode) + MudBlazor. Talks to the API exclusively through Refit typed clients in `Services/` (e.g. `ICustomersApiClient`), registered via `AddRefitGeneratedClient<T>()` with a base address resolved from Aspire service discovery (`services__invoicebuilder-api__http__0`) falling back to an `ApiBaseUrl` config value. Pages live in `Components/Pages/`; feature-specific dialogs/components are grouped under `Components/{Feature}/` (e.g. `Components/Customers/CustomerFormDialog.razor`). The live navigation is `MainLayout.razor`'s `MudNavMenu` — `Components/Layout/NavMenu.razor` is stale/unused Bootstrap markup, don't wire new links into it.

### Conventions to follow when adding a feature

Adding a new operation to an existing feature (or a new feature entirely) touches, in order: a Domain entity method if state changes → an Application `Models/*Dto.cs` + `Validation/*Validator.cs` + `{Op}.Handler.cs` (command/query + handler) + a `Shared/Responses/{Feature}/*Result.cs` → an Api `Features/{Feature}/{Op}/*.Endpoint.cs` mapped from the feature's `{Feature}Endpoint.cs` → (frontend, if needed) a Refit method on the relevant `I{Feature}ApiClient` and UI in `Components/`.

- Handlers return `Result<T>`; endpoints branch on `result.IsFailure` and return `Results.BadRequest(result.Error)` (or similar) rather than throwing.
- DTO validation is declarative FluentValidation (`RuleFor(...).NotEmpty()`, `.EmailAddress()`, etc.) wired in automatically by `ValidationFilter<T>` — don't hand-check DTOs in endpoints or handlers.
- List/paginated endpoints follow the cursor pattern in `CursorPagination.CreatePage`, not `Skip`/`Take` from the API surface.

## Code style

- Tabs for indentation in `.cs`/`.cshtml` (see [.editorconfig](.editorconfig)); spaces for `.md`/`.json`/`.csproj`/`.yml`.
- `Nullable` and `ImplicitUsings` are enabled solution-wide; `TreatWarningsAsErrors=true` with `AnalysisMode=All` plus Meziantou, SonarAnalyzer, and Roslynator analyzers ([Directory.Build.props](Directory.Build.props)) — a warning fails the build in every project **except** test projects, which explicitly opt out (`TreatWarningsAsErrors=false`) since analyzer rules tuned for product code are noisy on test code.
- Package versions are centrally managed in [Directory.Packages.props](Directory.Packages.props) — add new packages there, not with inline `Version=` attributes in a `.csproj`.
- Watch for SonarAnalyzer S1125 (no boolean-literal comparisons — use `!x` / `x.GetValueOrDefault()`, not `x != true`) and S1118/CA1852 (utility/non-inheritable classes need a private/protected constructor or `static`/`sealed`) — both are treated as build errors in product projects.
