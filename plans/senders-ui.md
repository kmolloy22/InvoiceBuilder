# Senders UI (list, create, edit, delete) in the Blazor app

_Drafted and implemented 2026-10-16 on branch `feature/senders-ui` (uncommitted when
written). All five phases complete and verified; see Final Recap._

Add a Senders page (`/senders`) with create, edit, and delete via a reusable
MudDialog form, calling the existing `/api/senders` endpoints, and add API
integration tests for the Senders endpoints (which have none today). This mirrors
`plans/customer-crud-ui.md` and reuses its patterns.

## For Future Agents

As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done,
set its status to `Complete` and write its **Phase Summary** (what was done, key
decisions, anything needed to continue with zero context); run the phase's
**Verification Plan** and record the result before moving on. When all phases are
done, fill in **Final Recap**, then tick the Senders UI milestone in `plans/PLAN.md`.

## Prerequisites

- The `fix/vulnerable-packages` PR must be merged first (or be the base branch).
  Until then the solution does not build (NuGet audit NU1902/NU1903 under
  `TreatWarningsAsErrors`).
- Docker Desktop running for the integration tests and for the AppHost smoke test.

## Decisions

**Decided 2026-10-16: decision 1 = option A; decisions 2, 3 and 4 = the defaults.**
Options as originally presented:

1. **Deleting a sender that has invoices** (see "Delete behavior" below).
   - **A (recommended):** `DeleteSenderHandler` checks for referencing invoices and
     returns `ResultErrorType.Conflict`; the endpoint maps `Conflict` to `409`; the
     UI shows "This sender is used by N invoice(s) and cannot be deleted." Small
     backend change, covered by a test.
   - B: UI-only — show the generic "Delete failed (500)" snackbar. No backend change,
     poor message. This is what Customers does today.
   - C: leave as is and document it.
2. **Create-then-list behavior.** The list is ordered by `Id`, and `Id` is a random
   `Guid.NewGuid()`, so a new row has no meaningful "top" position.
   - **Default:** match Customers — insert the new row at the top of the loaded page
     in memory (it moves to its real position on the next page load; the page can
     briefly show more than `PageSize` rows).
   - Alternative: reload the current page after create/delete (always correct, one
     extra request, new row may land on another page).
3. **Test fixture.** `CustomerApiFactory` is Customers-named and is an
   `IClassFixture`, so each test class starts its own Postgres container (~10 s).
   - **Default:** reuse it unchanged for `SendersEndpointsTests` (second container).
   - Alternative: rename to a neutral `ApiFactory` and share one container via an
     xUnit collection fixture (touches the existing Customers tests).
4. **Duplicated dialog plumbing.** `MapValidationErrors` / required-field markup is
   copy-pasted in `CustomerFormDialog.razor`; Senders would be a second copy and
   Invoices a third.
   - **Default:** copy for now, leave Customers untouched, note the debt.
   - Alternative: extract a small shared helper first (touches the Customers dialog).

## Context / facts (verified in the code, 2026-10-16)

- API is complete: `POST /api/senders`, `GET /api/senders` (cursor-paged),
  `GET /api/senders/{id:guid}`, `PUT /api/senders/{id:guid}`, `DELETE /api/senders/{id}`.
- Fields — all five required (`NotEmpty()` in both Create and Update validators),
  no length or format rules: `SenderCompanyName`, `SenderFullName`, `SenderAddress`,
  `SenderTaxVatId`, `BankDetails`. No email field. Property names are PascalCase and
  are the keys in `ValidationProblemDetails.errors`.
- `GET /api/senders` takes `GetSendersDto(Guid? Cursor, bool? IsNextPage, int PageSize = 5)`
  and returns `GetSendersResult(Items, NextCursor, PreviousCursor, PageSize)`; the
  handler clamps `PageSize` to 1..5. `SenderListItem` carries only `Id`,
  `SenderCompanyName`, `SenderFullName`, `SenderTaxVatId` — **not** `SenderAddress`
  or `BankDetails`, so the edit flow must fetch the full record via `GET /{id}`.
- Responses: `POST` → `201` with `CreateSenderResponseDto(Id, Location, CreatedAt)`;
  `Id` and `Location` use the **`N` format (no dashes)**, which `Guid.Parse` accepts.
  `PUT` → `200` with `UpdateSenderResult` (full 6-field record) — despite the
  endpoint's `Produces(204)` annotation, which is wrong. `DELETE` → `204`. Missing id
  → `404` for GET/PUT/DELETE. Validation failures → `400` `ValidationProblemDetails`.
- Seed data: 2 senders, each referenced by seeded invoices. `Invoice → Sender` is
  `DeleteBehavior.Restrict`.
- Frontend: `MainLayout.razor`'s `MudNavMenu` already has a Senders link to
  `/senders` marked `Disabled="true"` — this plan enables it. `NavMenu.razor` is
  stale; do not use it.
- `Program.cs` registers `ICustomersApiClient` explicitly with the resolved base
  address, 30 s timeout and `AddStandardResilienceHandler()`. A new
  `ISendersApiClient` needs its own registration (unlike Customers, where the
  interface already existed).
- No auth, no optimistic concurrency (last-write-wins).
- Out of scope: invoice UI, sender search/filter/sorting, bulk actions, bUnit tests,
  CI changes, changing Customers behavior, masking/encrypting `BankDetails`.

### Delete behavior (risk)

Deleting a sender referenced by an invoice violates the `Restrict` FK. The delete
handler has no guard, so the `DbUpdateException` is expected to surface as a generic
`500` from `GlobalExceptionHandler` (inferred from the code, **not yet run**).
Both seeded senders are in this state, so the first Delete click on a seeded row
would hit it. Customers has the same gap (a customer with invoices) — not fixed
here; recorded as a follow-up.

## Phase 1: Refit client + DI

Status: Complete   <!-- Not started | In progress | Complete -->

- [x] Create `frontend/src/InvoiceBuilder.Web/Services/ISendersApiClient.cs`:
  - [x] `[Get("/api/senders")] Task<GetSendersResult> GetSendersAsync([Query] GetSendersDto request)`
  - [x] `[Post("/api/senders")] Task<CreateSenderResponseDto> CreateSenderAsync([Body] CreateSenderDto request)`
  - [x] `[Get("/api/senders/{id}")] Task<GetSenderResult> GetSenderByIdAsync(Guid id)`
  - [x] `[Put("/api/senders/{id}")] Task<UpdateSenderResult> UpdateSenderAsync(Guid id, [Body] UpdateSenderDto request)`
  - [x] `[Delete("/api/senders/{id}")] Task DeleteSenderAsync(Guid id)`
- [x] Register in `Program.cs` with the same base address, timeout and
      `AddStandardResilienceHandler()` as the Customers client (avoid copy-pasting
      the whole block if a tiny local helper reads cleanly).

### Verification Plan

- `dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release`
  → `Build succeeded`, `0 Error(s)`; no Refit generator diagnostics.

### Phase Summary

Done 2026-10-16.

**What was done:** Added `Services/ISendersApiClient.cs` with five methods
(`GetSendersAsync`, `CreateSenderAsync`, `GetSenderByIdAsync`, `UpdateSenderAsync`,
`DeleteSenderAsync`). In `Program.cs`, replaced the inline Customers client
registration with a small local `ConfigureApiClient(IHttpClientBuilder)` helper
(base address, 30 s timeout, `AddStandardResilienceHandler()`) and used it for both
`ICustomersApiClient` and `ISendersApiClient`, so the two cannot drift.

**Verification result:** `dotnet build` of the Web project — `0 Error(s)`; the only
warnings are the pre-existing backend `CA1852` ones.

## Phase 2: `SenderFormDialog` component

Status: Complete

- [x] Create `frontend/src/InvoiceBuilder.Web/Components/Senders/SenderFormDialog.razor`
      modelled on `CustomerFormDialog.razor` (the live second block, not the
      commented-out first one).
- [x] `[Parameter] Guid? SenderId` — `null` = create, non-null = edit; edit mode
      fetches via `GetSenderByIdAsync` with a progress indicator; on failure
      snackbar + `MudDialog.Cancel()`.
- [x] Fields, all required with per-field messages: Company Name, Full Name,
      Tax / VAT ID, Address (multi-line), Bank Details (multi-line — the seed data
      is a long "Bank / IBAN / SWIFT" string). No email rule.
- [x] Mark every required field consistently (Customers' optional-looking Email label
      lacks the `*`; do not repeat that).
- [x] Submit: build `CreateSenderDto` / `UpdateSenderDto`; success → snackbar
      "Sender saved" and `DialogResult.Ok(SenderListItem)`. Create builds the row
      from the submitted values + `Guid.Parse(created.Id)`; edit from
      `UpdateSenderResult`.
- [x] `ApiException` 400 → map `errors` onto fields (keys match model property
      names) + error snackbar, dialog stays open; other failures → generic snackbar.
- [x] Disable Save/Cancel with a spinner while saving.

### Verification Plan

- `dotnet build frontend/src/InvoiceBuilder.Web/InvoiceBuilder.Web.csproj -c Release`
  → `0 Error(s)`.

### Phase Summary

Done 2026-10-16.

**What was done:** Added `Components/Senders/SenderFormDialog.razor`, modelled on the
live `CustomerFormDialog` block. `SenderId` parameter selects create vs edit; edit
fetches the full record. Five required fields, each with `Required` + a specific
message and a visible `*` (Address and Bank Details are multi-line; Bank Details has a
helper text). 400 responses are mapped onto fields from `ValidationProblemDetails`
(keys match the model property names). Returns a `SenderListItem` via
`DialogResult.Ok`.

**Notes:** `MapValidationErrors` is a copy of the Customers one (decision 4, default).
The `@bind-IsValid` / `_isValid` field from the Customers dialog was not carried over —
it is unused there.

**Verification result:** Web project build — `0 Error(s)`.

## Phase 3: `Senders.razor` page and navigation

Status: Complete

- [x] Create `frontend/src/InvoiceBuilder.Web/Components/Pages/Senders.razor`
      (`@page "/senders"`, `@rendermode InteractiveServer`) modelled on
      `Customers.razor`: header + **New Sender** button, `MudTable<SenderListItem>`
      (Company, Full Name, Tax / VAT ID, Actions), loading bar, error alert,
      "No senders found." empty state, Previous/Next cursor paging with page number
      (`PageSize = 5`).
- [x] **Edit** opens the dialog with `SenderId`; replace the row in place on Ok.
- [x] **Delete** → `ShowMessageBoxAsync` confirmation (`!confirmed.GetValueOrDefault()`
      — S1125 forbids `!= true`), then `DELETE`, remove the row, success snackbar;
      `ApiException` → error snackbar (409 message per decision 1).
- [x] Create inserts per decision 2.
- [x] In `Components/Layout/MainLayout.razor`, remove `Disabled="true"` from the
      Senders `MudNavLink`.

### Verification Plan

- `dotnet build InvoiceBuilder.slnx -c Release` → `0 Error(s)`.
- Manual smoke via `dotnet run --project backend/src/InvoiceBuilder.AppHost`
  (Docker running), at `/senders`:
  - New Sender → fill → Save → row appears, success snackbar.
  - Blank required fields → per-field errors, dialog stays open.
  - Edit → form pre-filled (including Address and Bank Details) → Save → row updates.
  - Delete a **newly created** sender → confirm → row disappears.
  - Delete a **seeded** sender (has invoices) → behavior matches decision 1.
  - Next / Previous page; Previous disabled on page 1, Next disabled on the last page.
  - Sidebar "Senders" link is enabled and highlights on `/senders`.

### Phase Summary

Done 2026-10-16.

**What was done:** Added `Components/Pages/Senders.razor` (`/senders`,
interactive server) modelled on `Customers.razor`: New Sender button, table (Company,
Full Name, Tax / VAT ID, Actions), loading bar, error alert, empty state,
Previous/Next cursor paging (`PageSize = 5`), edit and delete with a confirmation box.
Create inserts the new row at the top of the loaded page (decision 2, default). A
`409` on delete shows the API's message as a warning snackbar; other failures show the
generic error snackbar. Removed `Disabled="true"` from the Senders `MudNavLink` in
`MainLayout.razor`.

**Verification result — manual smoke via AppHost, all PASS:**
- New Sender → row at top, "Sender saved" snackbar.
- Blank form → five per-field errors, dialog stays open; all five labels show `*`.
- Edit → form pre-filled incl. Address and Bank Details; changing the company name and
  clicking Save directly persisted (confirmed in the list and via the API).
- Delete a newly created sender → confirmation names it, row removed, "Sender deleted".
- Delete seeded "Invoice Builder LLC" → warning snackbar *Sender "Invoice Builder LLC"
  is used by 1 invoice(s) and cannot be deleted.*, row stays.
- Paging with 7 senders (4 temporary ones added via the API, then deleted): page 1 = 5
  rows, page 2 = 2 rows; Previous disabled on page 1, Next disabled on the last page;
  Previous returns the identical first page.
- Sidebar "Senders" link enabled and highlighted on `/senders`.
- Browser console clean; dev DB restored to its original 3 senders afterwards.

**Not manually verified:** server-side 400 mapping in the UI (covered at API level by
`Post_invalid_returns_400_with_validation_errors`), network failures, and the edit/delete
404 paths in the UI.

## Phase 4: Delete guard for senders with invoices (only if decision 1 = A)

Status: Complete

- [x] `DeleteSender.Handler.cs`: before removing, `AnyAsync` on `Invoices` for the
      sender id; if any, return `Result<Success>.Failure(new ResultError(
      "SenderInUse", "...", ResultErrorType.Conflict))`.
- [x] `DeleteSender.Endpoint.cs`: branch on `result.Error.Type` — `NotFound` → 404,
      `Conflict` → 409 (`Results.Conflict(result.Error)`); add `Produces(409)`.
- [x] UI: surface the 409 message in the delete snackbar.
- [x] Do **not** change Customers delete in this plan; record it as a follow-up.

### Verification Plan

- Covered by the Phase 5 conflict test and the Phase 3 seeded-sender smoke step.

### Phase Summary

Done 2026-10-16.

**What was done:** `DeleteSender.Handler.cs` now counts referencing invoices before
removing; if any exist it returns `ResultError("SenderInUse", ..., ResultErrorType.Conflict)`
with the message `Sender "<company>" is used by N invoice(s) and cannot be deleted.`
`DeleteSender.Endpoint.cs` maps `Conflict` → `409` (`Results.Conflict`) and everything
else to `404`, adds `Produces<object>(409)`, and the description mentions the 409.

**Confirmed behavior:** with the guard temporarily reverted, the new conflict test
returned `500` (`Expected: Conflict, Actual: InternalServerError`) — confirming the
inferred FK-violation → 500 behavior; with the guard, `409`.

**Known limitation:** check-then-delete is not atomic. An invoice created for the
sender between the check and the delete would still hit the FK and return a `500`.
Acceptable for now (no concurrent invoice creation UI yet).

## Phase 5: Senders API integration tests

Status: Complete

- [x] `backend/tests/InvoiceBuilder.Api.IntegrationTests/SendersEndpointsTests.cs`
      (`IClassFixture<CustomerApiFactory>` unless decision 3 says otherwise), plain
      xUnit `Assert`, 4-space indent (test project convention):
  - [x] `Post_creates_sender_returns_201_and_location`
  - [x] `Post_invalid_returns_400_with_validation_errors` (e.g. `SenderCompanyName`,
        `BankDetails` keys)
  - [x] `Get_by_id_returns_sender` (all 5 fields round-trip)
  - [x] `Get_by_id_unknown_returns_404`
  - [x] `Put_updates_sender` (`200`, then `GET` reflects the changes)
  - [x] `Put_unknown_returns_404`
  - [x] `Delete_removes_sender` (`204`, then `GET` → `404`)
  - [x] `Delete_unknown_returns_404`
  - [x] `Get_list_pages_with_cursor` (Next returns different items; Previous returns
        to the first page)
  - [x] `Delete_sender_with_invoices_returns_409` (seeded sender; only if
        decision 1 = A — otherwise pin the current behavior)
- [x] Tests must not assume an empty database (seed data is present); create-then-act
      with fresh GUIDs.

### Verification Plan

- `dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests -c Release`
  → `Failed: 0` (Docker required); existing 8 Customers tests still pass.
- `dotnet build InvoiceBuilder.slnx -c Release` → `0 Error(s)`.

### Phase Summary

Done 2026-10-16.

**What was done:** Added `SendersEndpointsTests.cs` (`IClassFixture<CustomerApiFactory>`,
decision 3 default): the eight Customers-equivalent tests plus
`Get_list_pages_forward_and_back_with_cursor` and
`Delete_sender_with_invoices_returns_409_and_keeps_sender` (uses the seeded
"Invoice Builder LLC"). The paging test creates `PageSize + 2` senders so there are
always at least two pages, and asserts that Previous from page 2 returns exactly page 1.

**Notes:** the test project is not exempt from every analyzer rule — Roslynator
`RCS1128` failed the build once (`GetValueOrDefault(true)` → `?? true`).

**Verification result:**
```
$ dotnet test InvoiceBuilder.slnx -c Release
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18
$ dotnet build InvoiceBuilder.slnx -c Release
Build succeeded.  0 Error(s)
```
(8 existing Customers tests + 10 new Senders tests.)

## Final Recap

Senders now have a full management UI and test coverage. Summary of changes:

- **New:** `ISendersApiClient`, `SenderFormDialog.razor`, `Senders.razor` (`/senders`),
  `SendersEndpointsTests.cs`.
- **Changed:** `Program.cs` (shared client-registration helper), `MainLayout.razor`
  (Senders link enabled), `DeleteSender.Handler.cs` and `DeleteSender.Endpoint.cs`
  (409 when invoices reference the sender).
- **Behavior change to note:** `DELETE /api/senders/{id}` now returns `409` with a
  `ResultError` body for a sender that has invoices (previously a `500`).
- **Verification:** Release build clean; 18/18 integration tests pass; manual UI smoke
  test passed (see Phase 3).
- **Branch hygiene:** `feature/senders-ui` was cut from `chore/add-claude-skill` with the
  package-fix commit cherry-picked in as its base (the solution did not build without
  it). Rebase onto `master` once those two PRs are merged; the duplicate commits drop out.

## Follow-ups to record when done

- Customers delete has the same FK-restrict gap (customer with invoices).
- Sender/Customer lists are ordered by random `Guid` `Id`, not by name.
- `UpdateSenderEndpoint` annotations are wrong (`Produces(204)`; description mentions
  "first, last name and/or address") — documentation only, not fixed here.
- Duplicated dialog validation-mapping code across Customers/Senders (decision 4).
- Update `plans/PLAN.md`: tick Senders UI and the Senders integration-test part of
  the "More tests" milestone.

## Deployment Plan

_(fill in when all phases are done; expected to be a frontend change plus, if
decision 1 = A, a small API change with no schema or migration impact.)_
