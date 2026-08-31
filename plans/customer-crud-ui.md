# Customer Create / Edit / Delete in the Blazor UI

Add create, edit, and delete operations to the existing read-only Customers page
(`frontend/src/InvoiceBuilder.Web/Components/Pages/Customers.razor`) via a reusable
MudDialog form, calling the existing `/api/customers` endpoints, and stand up an
API integration test project (Testcontainers + PostgreSQL) covering those endpoints.

## For Future Agents

As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done,
set its status to `Complete` and write its **Phase Summary** (what was done, key
decisions, anything needed to continue with zero context); run the phase's
**Verification Plan** and record the result before moving on. When all phases are
done, fill in **Final Recap** and **Deployment Plan**.

### Context / decisions (from planning)

- API is complete and unchanged: `POST /api/customers`, `GET /api/customers/{id:guid}`,
  `PUT /api/customers/{id:guid}`, `DELETE /api/customers/{id}`. Fields (all required):
  `CompanyName`, `CustomerName`, `CustomerAddress`, `PostalCode`, `CustomerEmail`
  (email format), `CustomerTaxVatId`.
- `POST` returns `201 Created` with `CreateCustomerResponseDto` (`Id`, `Location`,
  `CreatedAt`). `PUT` returns `200 OK` with `UpdateCustomerResult`. `DELETE` returns
  `204 No Content`. Validation failures return `400` with `ValidationProblemDetails`.
- `CustomerListItem` (list rows) carries only `Id`, `CompanyName`, `CustomerName`,
  `CustomerEmail` — **not** enough to seed an edit form, so the edit flow must
  fetch the full record via `GET /api/customers/{id}`.
- Form UX: one reusable `MudDialog` component with a create mode and an edit mode,
  launched from a **New Customer** button and a per-row **Edit** action.
- Delete UX: `IDialogService.ShowMessageBox` confirmation, then `DELETE`.
- List refresh: mutate the loaded `_customers` list in place (add / replace /
  remove the single affected row) — no refetch.
- API errors: on `400`, parse `ValidationProblemDetails.errors` and show each
  message under its field in the `MudForm`, plus a generic error snackbar; keep
  the dialog open. Other failures: generic error snackbar.
- Success feedback: `ISnackbar` success message on each operation.
- Test DB: `Testcontainers.PostgreSql`, applying the real EF migrations, driven
  through `WebApplicationFactory<Program>`. Framework: xUnit.
- CI (`.github/workflows/ci.yml`) stays build-only — not modified by this work.
- No auth, no optimistic concurrency (last-write-wins, matching the API).
- Out of scope: invoice/sender UI, customer search/filter, bulk actions,
  bUnit/component tests, CI changes.

## Phase 1: Refit client methods + DI

Status: Complete   <!-- Not started | In progress | Complete -->

- [x] Add to `frontend/src/InvoiceBuilder.Web/Services/ICustomersApiClient.cs`:
  - [x] `[Post("/api/customers")] Task<CreateCustomerResponseDto> CreateCustomerAsync([Body] CreateCustomerDto request)`
  - [x] `[Get("/api/customers/{id}")] Task<GetCustomerResult> GetCustomerByIdAsync(Guid id)`
  - [x] `[Put("/api/customers/{id}")] Task<UpdateCustomerResult> UpdateCustomerAsync(Guid id, [Body] UpdateCustomerDto request)`
  - [x] `[Delete("/api/customers/{id}")] Task DeleteCustomerAsync(Guid id)`
- [x] Add `using` for `InvoiceBuilder.Application.Features.Customers.Models.Create`
      and `...Models.Update` as needed; reference `GetCustomerResult` /
      `UpdateCustomerResult` from `InvoiceBuilder.Application.Shared.Responses.Customers`.
- [x] Confirm no `Program.cs` change is required — `AddRefitGeneratedClient<ICustomersApiClient>()`
      already registers the interface; new methods ride the existing registration.
      (If the source-generated client needs a rebuild trigger, note it here.)

### Verification Plan

- `dotnet build InvoiceBuilder.slnx -c Release` → succeeds (Application DTOs still compile).
- `dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release`
  → `Build succeeded`, `0 Error(s)`; Refit source generator emits the client with
  4 new methods (no `RefitInternalNamespace`/`InvalidOperationException` generator errors in output).

### Phase Summary

Done 2026-08-31.

**What was done:** Added four methods to `ICustomersApiClient` —
`CreateCustomerAsync` (`[Post]` → `CreateCustomerResponseDto`),
`GetCustomerByIdAsync` (`[Get]` → `GetCustomerResult`),
`UpdateCustomerAsync` (`[Put]` → `UpdateCustomerResult`),
`DeleteCustomerAsync` (`[Delete]` → `Task`). Added `using`s for the
`...Models.Create` and `...Models.Update` namespaces; `GetCustomerResult` /
`UpdateCustomerResult` come from the already-imported
`InvoiceBuilder.Application.Shared.Responses.Customers`.

**Key decisions / notes for later phases:**
- No `Program.cs` change needed. `AddRefitGeneratedClient<ICustomersApiClient>()`
  registers the interface; the Refit source generator picked up the new methods on
  rebuild with no generator diagnostics.
- **Path correction:** the solution file is `InvoiceBuilder.slnx` at the repo root,
  not `backend/InvoiceBuilder.slnx`. The plan's verification commands (this phase and
  Phase 4) have been updated. Note `.github/workflows/ci.yml` still references the
  non-existent `backend/InvoiceBuilder.slnx` — out of scope here, but a future CI
  fix will be needed.
- `UpdateCustomerResult` is a full 6-field record (Id + all fields), so Phase 3 can
  build the updated list row straight from the `PUT` response without a follow-up GET.
- `CreateCustomerResponseDto` returns only `Id` / `Location` / `CreatedAt` (no field
  echo), so after create, Phase 3 builds the new `CustomerListItem` from the submitted
  form values + the returned `Id`.

**Verification result:**
```
$ dotnet build InvoiceBuilder.slnx -c Release --nologo
    22 Warning(s)    (all pre-existing CAxxxx analyzer warnings, none in changed file)
    0 Error(s)
Time Elapsed 00:01:05

$ dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release --nologo
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Phase 2: `CustomerFormDialog` component

Status: Complete

- [x] Create `frontend/src/InvoiceBuilder.Web/Components/Customers/CustomerFormDialog.razor`
      (+ `_Imports` coverage) as a `MudDialog` with a `[CascadingParameter] IMudDialogInstance`.
- [x] `[Parameter] Guid? CustomerId` — `null` = create mode, non-null = edit mode.
- [x] On `OnInitializedAsync`: edit mode → `await CustomersApiClient.GetCustomerByIdAsync(CustomerId.Value)`
      and populate a local editable model; show a `MudProgressCircular` while loading; on
      fetch failure show error snackbar and `MudDialog.Cancel()`.
- [x] `MudForm` with `MudTextField` bindings for all 6 fields; client-side `required`
      validation and an email rule on `CustomerEmail` mirroring `CreateCustomerDtoValidator`.
- [x] Submit: build `CreateCustomerDto` / `UpdateCustomerDto`; call the matching client
      method; on success `Snackbar.Add("Customer saved", Severity.Success)` and
      `MudDialog.Close(DialogResult.Ok(<saved row model>))`.
- [x] Catch `Refit.ApiException`: if `StatusCode == 400`, deserialize body to
      `HttpValidationProblemDetails` (or `ValidationProblemDetails`), map
      `errors[field]` messages into per-field `MudForm` errors via a
      `Func<string,IEnumerable<string>>` validation hook or a manual error dictionary;
      also `Snackbar.Add(..., Severity.Error)`; keep dialog open. Non-400 → generic
      error snackbar, keep dialog open.
- [x] Disable the Save button and show a spinner while the request is in flight.

### Verification Plan

- `dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release`
  → `Build succeeded`, `0 Error(s)`.
- `grep -n "GetCustomerByIdAsync\|ApiException\|DialogResult.Ok" frontend/src/InvoiceBuilder.Web/Components/Customers/CustomerFormDialog.razor`
  → all three present (edit fetch, error handling, and success result wiring exist).

### Phase Summary

Done 2026-08-31.

**What was done:** Added
`frontend/src/InvoiceBuilder.Web/Components/Customers/CustomerFormDialog.razor`
— a self-contained `MudDialog` used for both create and edit:
- `[Parameter] Guid? CustomerId` selects mode (`IsEdit => CustomerId.HasValue`).
- Edit mode fetches the full record in `OnInitializedAsync` via
  `GetCustomerByIdAsync`, shows a `MudProgressCircular` while `_loading`, and on
  failure raises an error snackbar + `MudDialog.Cancel()`.
- Private `CustomerFormModel` (6 strings) bound to six `MudTextField`s inside a
  `MudForm` (`@ref _form`). Client validation: `Required` on all, `EmailRule`
  (regex) on `CustomerEmail`, mirroring `CreateCustomerDtoValidator`.
- `Submit()` runs `await _form.ValidateAsync()`; on success calls
  `CreateCustomerAsync` / `UpdateCustomerAsync`, shows "Customer saved" snackbar,
  and closes with `DialogResult.Ok(CustomerListItem)` so Phase 3 can splice the
  row into the list.
- `catch (ApiException ex) when (ex.StatusCode == BadRequest)` → `MapValidationErrors`
  parses the RFC 7807 body (`JsonDocument`, reads `errors` object) into a
  `_serverErrors` dict keyed by PascalCase field name; each `MudTextField` binds
  `Error`/`ErrorText` from that dict. `_serverErrors` is cleared at the top of every
  `Submit`. Non-400 `ApiException` and generic `Exception` → error snackbar; dialog
  stays open in all failure paths.
- Save button disabled + spinner while `_saving`; Cancel disabled while saving.

**Key decisions / deviations:**
- Used **manual `Error`/`ErrorText` binding** for server-side errors (per-field
  dict) rather than a `Func<string,IEnumerable<string>>` validation hook — simpler
  and keeps client vs. server error sources separate. Acceptable per the plan's
  "manual error dictionary" option.
- `MudForm.Validate()` is `[Obsolete]`-as-error in MudBlazor 9.9.0 →
  used `ValidateAsync()`.
- Create response has no field echo, so the returned `CustomerListItem` for create
  is built from `Guid.Parse(created.Id)` + submitted form values; edit uses the
  full `UpdateCustomerResult`.
- No `_Imports` change needed — `Components/_Imports.razor` already covers MudBlazor;
  component-specific `@using`s (Refit, the DTO namespaces, `System.Text.Json`,
  `System.Net`) are declared in the file.
- Server error keys: `ValidationFilter<T>` groups by `PropertyName`, so keys arrive
  as `CompanyName`, `CustomerEmail`, etc. — a direct match to model property names,
  no remapping needed.

**Not done here (belongs to Phase 3):** the dialog is not yet opened from anywhere;
`Customers.razor` is untouched.

**Verification result:**
```
$ dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release --no-incremental --nologo
Build succeeded.
    19 Warning(s)   (all pre-existing backend CA1000/CA1014/CA1852; none in the new file)
    0 Error(s)

$ grep -n "GetCustomerByIdAsync\|ApiException\|DialogResult.Ok" .../CustomerFormDialog.razor
111:  var customer = await CustomersApiClient.GetCustomerByIdAsync(CustomerId!.Value);
181:  MudDialog.Close(DialogResult.Ok(row));
183:  catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
188:  catch (ApiException ex)
```

## Phase 3: Wire actions into `Customers.razor`

Status: Complete

- [x] Inject `IDialogService` and `ISnackbar` into `Components/Pages/Customers.razor`.
- [x] Add a **New Customer** `MudButton` above the table → opens `CustomerFormDialog`
      with no `CustomerId`; on `DialogResult` `Ok`, insert the returned row at the top
      of `_customers` and `StateHasChanged()`.
- [x] Add an actions `MudTd` per row with **Edit** and **Delete** `MudIconButton`s
      (update `<HeaderContent>` with a matching empty/"Actions" `MudTh`).
- [x] **Edit** → open `CustomerFormDialog` with `CustomerId = context.Id`; on `Ok`,
      replace the matching entry in `_customers` (match by `Id`) and `StateHasChanged()`.
- [x] **Delete** → `DialogService.ShowMessageBoxAsync("Delete customer", $"Delete \"{context.CompanyName}\"? ...", yesText:"Delete", cancelText:"Cancel")`;
      on confirm call `CustomersApiClient.DeleteCustomerAsync(context.Id)`, remove the
      row from `_customers`, success snackbar; on `ApiException` error snackbar.
- [x] Ensure `CustomerListItem` is the row model everywhere; the dialog returns a
      `CustomerListItem` directly, so no projection is needed.
- [x] Keep existing paging / "Load More" behavior intact (untouched).

### Verification Plan

- `dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release`
  → `Build succeeded`, `0 Error(s)`.
- Run the app via AppHost (`dotnet run --project backend/src/InvoiceBuilder.AppHost`),
  open `/customers`, and confirm the manual smoke checklist:
  - New Customer → fill form → Save → row appears at top, success snackbar.
  - Submit with a blank required field → per-field error shown, dialog stays open.
  - Edit a row → form pre-filled from `GET /{id}` → change a field → Save → row updates in place.
  - Delete a row → confirm box → row disappears, success snackbar.
  - "Load More" still pages as before.

### Phase Summary

Done 2026-08-31.

**What was done:** Reworked
`frontend/src/InvoiceBuilder.Web/Components/Pages/Customers.razor`:
- Added `@using InvoiceBuilder.Web.Components.Customers` and `@using Refit`; injected
  `IDialogService` and `ISnackbar`.
- Header row: wrapped the title in a `MudStack` (space-between) with a **New Customer**
  `MudButton` (`Icons.Material.Filled.Add`), disabled while `_loading`.
- Table: added an "Actions" `MudTh` (right-aligned) and a matching `MudTd` per row
  with **Edit** (`Icons.Material.Filled.Edit`) and **Delete**
  (`Icons.Material.Filled.Delete`) `MudIconButton`s, each with an `aria-label`.
- `OpenCreateDialog()` → `DialogService.ShowAsync<CustomerFormDialog>("New Customer")`;
  on `{ Canceled: false, Data: CustomerListItem created }` → `_customers.Insert(0, created)`
  + `StateHasChanged()`.
- `OpenEditDialog(item)` → `ShowAsync<CustomerFormDialog>("Edit Customer", new
  DialogParameters<CustomerFormDialog> { { x => x.CustomerId, item.Id } })`; on Ok →
  `_customers.FindIndex(c => c.Id == updated.Id)` then replace in place +
  `StateHasChanged()`.
- `DeleteCustomer(item)` → `ShowMessageBoxAsync(...)`; if confirmed, call
  `DeleteCustomerAsync(item.Id)`, `_customers.RemoveAll(c => c.Id == item.Id)`,
  "Customer deleted" success snackbar; `catch (ApiException)` / `catch (Exception)` →
  error snackbar. Dialog-cancel path returns without side effects.
- `LoadCustomers` / "Load More" / cursor paging untouched.

**Key decisions / deviations:**
- **`ShowMessageBox` → `ShowMessageBoxAsync`.** MudBlazor 9.9.0 renamed the extension;
  the old name doesn't compile. Same positional/named params (`yesText`, `cancelText`),
  returns `Task<bool?>`.
- **`if (confirmed != true)` → `if (!confirmed.GetValueOrDefault())`.** The repo's
  SonarAnalyzer (S1125, warnings-as-errors) rejects boolean-literal comparisons.
- Dialog returns a `CustomerListItem`, so the list stays strongly typed with no
  mapping layer. For *create*, that item is `new CustomerListItem(Guid.Parse(created.Id),
  <submitted CompanyName/CustomerName/CustomerEmail>)` (create response has no field
  echo — decided in Phase 1).
- In-place list mutation only (`Insert` / indexer / `RemoveAll`) + explicit
  `StateHasChanged()`; no refetch, matching the plan.

**Verification result:**
- Autonomous build check — PASS:
  ```
  $ dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release --no-incremental --nologo
  Build succeeded.
      0 Error(s)
  $ dotnet build InvoiceBuilder.slnx -c Release --nologo
  Build succeeded.
      0 Error(s)
  ```
- Manual smoke checklist — **NOT RUN in this session.** It needs an interactive
  `dotnet run --project backend/src/InvoiceBuilder.AppHost` with Docker Desktop
  available (Aspire starts Postgres + pgAdmin containers, the API, and the web app),
  which is outside this session's autonomous scope. **Action for the reviewer:** run
  the AppHost, open `/customers`, and walk the five checklist items above before
  treating Phase 3 as field-verified.

### Follow-up noticed (out of scope for this plan)

`Components/Layout/NavMenu.razor` (the Bootstrap nav) still lists a stale "Counter"
link and no "Customers"/"Invoices" entries — but it is not the active nav. The live
nav is `MainLayout.razor`'s `MudNavMenu`, which already has `/customers` enabled.
No action needed for this work.

## Phase 4: API integration test project

Status: Complete

- [x] Create `backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj`
      (net10.0), referencing `InvoiceBuilder.Api`, with packages: `xunit`,
      `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
      `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`.
      (`FluentAssertions` not used — plain `Assert`.) Added the project to
      `InvoiceBuilder.slnx` under a new `/Tests/` folder.
- [x] Ensure `InvoiceBuilder.Api` exposes its `Program` to the test assembly —
      appended `public partial class Program { protected Program() { } }` at the end
      of `Program.cs`.
- [x] `CustomerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime` — starts a
      `PostgreSqlContainer` and points the API at it via
      `UseSetting("ConnectionStrings:InvoiceBuilderDB", …)`. Migrations + seed run
      automatically because `Program` already calls `await app.InitializeDbAsync()`
      before `RunAsync()`. `HttpClient` via `factory.CreateClient()`. Container +
      base factory disposed in an explicit `IAsyncLifetime.DisposeAsync()`.
- [x] Tests (`CustomersEndpointsTests`, `IClassFixture<CustomerApiFactory>`):
  - [x] `Post_creates_customer_returns_201_and_location`
  - [x] `Post_invalid_returns_400_with_validation_errors` (asserts `CompanyName` +
        `CustomerEmail` keys in `HttpValidationProblemDetails.Errors`).
  - [x] `Get_by_id_returns_customer` (all 6 fields round-trip).
  - [x] `Get_by_id_unknown_returns_404`.
  - [x] `Put_updates_customer` (`200`, then `GET` reflects the changes).
  - [x] `Put_unknown_returns_404`.
  - [x] `Delete_removes_customer` (`204`, then `GET` → `404`).
  - [x] `Delete_unknown_returns_404`.

### Verification Plan

- `dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj -c Release`
  → all tests pass; console shows `Passed! - Failed: 0` with 8 tests run.
  (Requires Docker running locally for Testcontainers.)
- `dotnet build InvoiceBuilder.slnx -c Release` → still `0 Error(s)` with the
  new project included in the solution.

### Phase Summary

Done 2026-08-31.

**What was done:**
- `Directory.Packages.props`: added `Microsoft.AspNetCore.Mvc.Testing` 10.0.0 and
  `Testcontainers.PostgreSql` 4.6.0 (central package management).
- `backend/src/InvoiceBuilder.Api/Program.cs`: appended a global-namespace
  `public partial class Program { protected Program() { } }` so
  `WebApplicationFactory<Program>` can find the entry point. The `protected`
  constructor keeps SonarAnalyzer S1118 quiet (API project is warnings-as-errors).
- New project `backend/tests/InvoiceBuilder.Api.IntegrationTests/` with three files:
  - `InvoiceBuilder.Api.IntegrationTests.csproj` — `Microsoft.NET.Sdk`, net10.0,
    `IsTestProject`, `TreatWarningsAsErrors=false` (see deviation), the 5 test
    packages, project reference to the API.
  - `CustomerApiFactory.cs` — `WebApplicationFactory<Program>` + `IAsyncLifetime`;
    `PostgreSqlBuilder().WithImage("postgres:16-alpine")`; `ConfigureWebHost` sets
    `UseEnvironment("Development")` and
    `UseSetting("ConnectionStrings:InvoiceBuilderDB", _database.GetConnectionString())`.
  - `CustomersEndpointsTests.cs` — 8 `[Fact]`s using `System.Net.Http.Json`
    helpers and the real Application DTOs (`CreateCustomerDto`, `UpdateCustomerDto`,
    `GetCustomerResult`, `CreateCustomerResponseDto`).
- `InvoiceBuilder.slnx`: added `/Tests/` folder with the new project.

**Key decisions / deviations:**
- **No explicit migrate call in the fixture.** `Program` already runs
  `InitializeDbAsync()` (migrate + seed) before `RunAsync()`, and
  `WebApplicationFactory` executes everything up to `RunAsync()`, so the container DB
  is migrated and seeded by the time `CreateClient()` returns. Simpler than the
  plan's "run `db.Database.MigrateAsync()` in the fixture", and it also exercises the
  real startup path. Tests don't assume an empty DB (all create-then-act on fresh
  GUIDs), so the 10 seeded customers are harmless.
- **Connection string override via `UseSetting`** (not an env var). The Aspire
  `AddAzureNpgsqlDbContext` reads `ConnectionStrings:InvoiceBuilderDB`; `UseSetting`
  feeds it with highest precedence. Confirmed working (all tests green).
- **`TreatWarningsAsErrors=false` for the test project only.** The repo-wide
  `Directory.Build.props` sets it `true` plus four analyzer packages; test code trips
  their style rules. Documented in the csproj. Product projects are unchanged.
- **Image `postgres:16-alpine`** pinned explicitly rather than the Testcontainers
  default, for a small deterministic pull.
- **`NU1903` (SSH.NET 2024.2.0 high-severity advisory)** comes in transitively via
  `Testcontainers` 4.6.0. Surfaces as a restore warning on the test project only
  (not fatal). Follow-up: bump when Testcontainers ships a patched transitive, or add
  a direct `SSH.NET` pin. Does not affect product code.

**Verification result — PASS:**
```
$ dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj -c Release
Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8, Duration: 13 s

$ dotnet build InvoiceBuilder.slnx -c Release
Build succeeded.
    0 Error(s)
```

## Final Recap

All four phases complete (2026-08-31). Customer create / edit / delete is now
available in the Blazor UI, backed by the existing API, with an integration test
project covering the endpoints it uses.

**Shipped:**
1. `ICustomersApiClient` gained `CreateCustomerAsync`, `GetCustomerByIdAsync`,
   `UpdateCustomerAsync`, `DeleteCustomerAsync` (no `Program.cs`/DI change — the
   existing `AddRefitGeneratedClient` registration covers them).
2. `Components/Customers/CustomerFormDialog.razor` — one `MudDialog` for create and
   edit: edit mode fetches the full record via `GetCustomerByIdAsync`; `MudForm`
   with client validation mirroring `CreateCustomerDtoValidator`; on `400` it maps
   `ValidationProblemDetails.errors` onto per-field messages + an error snackbar and
   stays open; success closes with `DialogResult.Ok(CustomerListItem)`.
3. `Components/Pages/Customers.razor` — **New Customer** button, per-row **Edit** /
   **Delete** actions, `ShowMessageBoxAsync` delete confirmation, success/error
   snackbars, and in-place list mutation (`Insert` / indexer / `RemoveAll` +
   `StateHasChanged`); paging untouched.
4. `backend/tests/InvoiceBuilder.Api.IntegrationTests/` — `WebApplicationFactory` +
   Testcontainers PostgreSQL, 8 passing tests over POST / GET-by-id / PUT / DELETE
   `/api/customers` (happy path, validation `400`, and `404`s).

**Files added:** `CustomerFormDialog.razor`, the test project (3 files),
`plans/customer-crud-ui.md`.
**Files changed:** `ICustomersApiClient.cs`, `Customers.razor`, `Program.cs`
(partial-class shim), `Directory.Packages.props`, `InvoiceBuilder.slnx`.
**Not changed:** the API/Application/Domain/DB projects (no backend behavior
change), `.github/workflows/ci.yml` (still build-only, by decision).

**Known follow-ups (out of scope, recorded above):**
- `.github/workflows/ci.yml` references `backend/InvoiceBuilder.slnx` which does not
  exist (the solution is `InvoiceBuilder.slnx` at the repo root) — CI is likely
  already red for an unrelated reason.
- `NU1903` SSH.NET advisory via Testcontainers 4.6.0 (test project only).
- `Components/Layout/NavMenu.razor` is stale Bootstrap markup and unused; the live
  nav (`MainLayout.razor` `MudNavMenu`) already links `/customers`.
- The **manual UI smoke checklist in Phase 3 was not run** in the implementing
  session — needs an interactive AppHost run (see Deployment Plan step 3).

## Deployment Plan

No schema or API changes — this is a frontend-only change plus a new test project.
Standard deploy of the `InvoiceBuilder.Web` app.

1. **Merge & build.**
   - `git checkout -b feature/customer-crud-ui` (if not already on a branch),
     commit all changed/added files, open a PR to `master`.
   - CI runs `dotnet build` (build-only by decision). Expect green once the
     pre-existing `ci.yml` slnx-path issue is unrelated / resolved.
2. **Full local verification before merge.**
   - `dotnet build InvoiceBuilder.slnx -c Release` → `0 Error(s)`.
   - `dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj -c Release`
     with Docker running → `Failed: 0, Passed: 8`.
3. **Manual UI smoke (Phase 3 checklist, still outstanding).**
   - `dotnet run --project backend/src/InvoiceBuilder.AppHost` (Docker required;
     Aspire starts Postgres + pgAdmin + API + web).
   - Open the web app's `/customers` and confirm: New Customer → save → row appears;
     blank required field → inline error, dialog stays open; Edit → form prefilled →
     save → row updates; Delete → confirm → row removed; "Load More" still pages.
4. **Deploy the web app** exactly as today (no new env vars, no migration step —
   the API and its database are unchanged). The new `ApiBaseUrl` / service-discovery
   config that `InvoiceBuilder.Web` already relies on is unchanged.
5. **Rollback:** revert the PR. No data migration to undo; the API is untouched.
