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

Status: Not started

- [ ] Inject `IDialogService` and `ISnackbar` into `Components/Pages/Customers.razor`.
- [ ] Add a **New Customer** `MudButton` above the table → opens `CustomerFormDialog`
      with no `CustomerId`; on `DialogResult` `Ok`, insert the returned row at the top
      of `_customers` and `StateHasChanged()`.
- [ ] Add an actions `MudTd` per row with **Edit** and **Delete** `MudIconButton`s
      (update `<HeaderContent>` with a matching empty/"Actions" `MudTh`).
- [ ] **Edit** → open `CustomerFormDialog` with `CustomerId = context.Id`; on `Ok`,
      replace the matching entry in `_customers` (match by `Id`) and `StateHasChanged()`.
- [ ] **Delete** → `DialogService.ShowMessageBox("Delete customer", $"Delete {context.CompanyName}?", yesText:"Delete", cancelText:"Cancel")`;
      on confirm call `CustomersApiClient.DeleteCustomerAsync(context.Id)`, remove the
      row from `_customers`, success snackbar; on `ApiException` error snackbar.
- [ ] Ensure `CustomerListItem` is the row model everywhere; if the dialog returns a
      fuller model, project it back to `CustomerListItem` for the list.
- [ ] Keep existing paging / "Load More" behavior intact.

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

_(write when phase completes)_

## Phase 4: API integration test project

Status: Not started

- [ ] Create `backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj`
      (net10.0), referencing `InvoiceBuilder.Api`, with packages: `xunit`,
      `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
      `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`,
      `FluentAssertions` (optional). Add the project to `backend/InvoiceBuilder.slnx`.
- [ ] Ensure `InvoiceBuilder.Api` exposes its `Program` to the test assembly
      (add `public partial class Program { }` at the end of `Program.cs` if the
      implicit `Program` is not accessible).
- [ ] `CustomerApiFixture : IAsyncLifetime` — start a `PostgreSqlContainer`, build a
      `WebApplicationFactory<Program>` that overrides the `InvoiceBuilderDB`
      connection string to the container (env var
      `ConnectionStrings__InvoiceBuilderDB` / `services__...`), run EF migrations
      (`db.Database.MigrateAsync()`), and expose an `HttpClient`. Dispose both.
- [ ] Tests (`CustomersEndpointsTests`):
  - [ ] `Post_creates_customer_returns_201_and_location` — valid `CreateCustomerDto`
        → `201`, body has non-empty `Id`, `Location` == `/api/customers/{id}`.
  - [ ] `Post_invalid_returns_400_with_validation_errors` — blank `CompanyName` /
        bad email → `400`, `ValidationProblemDetails.errors` contains those keys.
  - [ ] `Get_by_id_returns_customer` — create then `GET /{id}` → `200`, all 6 fields round-trip.
  - [ ] `Get_by_id_unknown_returns_404`.
  - [ ] `Put_updates_customer` — create, `PUT` changed fields → `200`; subsequent
        `GET` reflects the changes.
  - [ ] `Put_unknown_returns_404`.
  - [ ] `Delete_removes_customer` — create, `DELETE` → `204`; subsequent `GET` → `404`.
  - [ ] `Delete_unknown_returns_404`.

### Verification Plan

- `dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests/InvoiceBuilder.Api.IntegrationTests.csproj -c Release`
  → all tests pass; console shows `Passed! - Failed: 0` with 8 tests run.
  (Requires Docker running locally for Testcontainers.)
- `dotnet build InvoiceBuilder.slnx -c Release` → still `0 Error(s)` with the
  new project included in the solution.

### Phase Summary

_(write when phase completes)_

## Final Recap

_(write when all phases complete: summary of the entire piece of work)_

## Deployment Plan

_(write when all phases complete: step-by-step deployment instructions)_
