# InvoiceBuilder — Project Plan

_Last updated: 2026-11-18_

This document is the single source of truth for InvoiceBuilder's direction.
Read it before starting work; update it whenever scope, milestones, or
architecture decisions change.

## Goal

A full-stack web application for creating, managing, and issuing invoices.
Users maintain a book of customers and sender profiles, assemble invoices
from line items, and export them (PDF) for delivery.

## Architecture

- **Solution:** `InvoiceBuilder.slnx`, .NET, .NET Aspire orchestration
  (`InvoiceBuilder.AppHost`, `InvoiceBuilder.ServiceDefaults`).
- **Backend** (`backend/src`):
  - `InvoiceBuilder.Domain` — entities (`Customer`, `Sender`, `Invoice`,
    `InvoiceLineItem`) with encapsulated factory/`Update` methods and a
    `Result<T>` pattern.
  - `InvoiceBuilder.Application` — vertical-slice features (Customers,
    Senders, Invoices) using MediatR + FluentValidation.
  - `InvoiceBuilder.Api` — minimal-API endpoints grouped per feature;
    cursor-based pagination on list endpoints.
  - `InvoiceBuilder.Database` (assembly `InvoiceBuilder.Infrastructure.Database`)
    — EF Core + PostgreSQL, configurations and migrations.
  - `InvoiceBuilder.Shared.Kernel` — intended for cross-cutting primitives;
    currently an empty placeholder that nothing references.
- **Frontend** (`frontend/src/InvoiceBuilder.Web`):
  - Blazor Server, MudBlazor component library.
  - Refit-based typed API clients in `Services/`.
- **Tests** (`backend/tests`):
  - `InvoiceBuilder.Api.IntegrationTests` — xUnit + `WebApplicationFactory`
    against a Testcontainers PostgreSQL (requires Docker).
- **CI** (`.github/workflows/ci.yml`): restore, build and test the root
  `InvoiceBuilder.slnx` on push/PR to `master`. Frontend job is commented out.

## Current status

| Area                    | API           | Frontend      |
| ----------------------- | ------------- | ------------- |
| Customers               | Full CRUD     | Full CRUD + Previous/Next paging |
| Senders                 | Full CRUD     | Full CRUD + Previous/Next paging |
| Invoices                | Full CRUD     | Create (builder UI), list + detail view; no edit UI yet |
| Line items              | Via Invoice   | Create only, via the invoice builder |

- EF migrations: `InitialCreate`, `AddInvoice`.
- Tests: 35 API integration tests — Customers (9: POST, GET by id, PUT, DELETE;
  happy paths, validation 400, 404s, and 409 on deleting a customer that has
  invoices), Senders (10: the same plus cursor paging), and Invoices (16:
  POST/GET-by-id/PUT/DELETE, validation 400s, duplicate-`InvoiceNumber` and
  unknown-`CustomerId`/`SenderId` business-rule failures on both create and
  update, cursor paging — see `InvoicesEndpointsTests.cs`). No Application
  handler tests, no frontend tests (`frontend/tests` is empty).
- `DELETE /api/customers/{id}` and `DELETE /api/senders/{id}` both return 409
  when invoices reference the row (`plans/senders-ui.md`; Customers parity
  added alongside the invoice builder work).
- Invoice builder UI at `/invoices/new` (`Components/Pages/NewInvoice.razor`):
  customer/sender pickers (paged through client-side to list all rows),
  editable line items with live subtotal/tax/total, submits via a new
  `IInvoicesApiClient`. Manually smoke-tested end to end in the browser
  (create, add/remove line items, submit, form reset) on 2026-11-18 — passed.
  No edit support yet (see known issues).
- Invoice list + detail UI: `/invoices` (`Components/Pages/Invoices.razor`,
  Previous/Next paging, delete with confirm) and `/invoices/{id}`
  (`Components/Pages/InvoiceDetail.razor`, read-only: dates, currency, notes,
  customer/sender, line items, subtotal/tax/total). Manually smoke-tested
  (list, view detail, delete) on 2026-11-18 — passed. No edit UI yet (see
  known issues).
- Invoice PDF export: `GET /api/invoices/{id}/pdf` (`GetInvoicePdfHandler` +
  `InvoicePdfDocument`, built with QuestPDF under the Community license —
  free under $1M USD annual revenue) renders invoice number, dates, sender/
  customer, line items, and subtotal/tax/total to a single-page PDF. The Web
  project proxies the download at `/invoices/{id}/pdf` (can't call the Api
  directly from the browser) so `InvoiceDetail.razor`'s "Download PDF" button
  can link straight to it. Manually verified end to end (direct API curl +
  through the Web proxy in the browser) on 2026-11-18 — passed. No automated
  test coverage yet (see Known issues).
- Customers UI manually smoke-tested end to end on 2026-10-16 (create,
  validation errors, edit, delete, paging) — all passed.
- Build: both known NuGet audit failures are fixed and merged to `master` —
  the `System.Security.Cryptography.Xml`/`Pkcs` issue, and the later
  Aspire/MessagePack issue in `InvoiceBuilder.AppHost`
  (`Aspire.Dashboard.Sdk.win-x64`, `Aspire.Hosting`, `Aspire.Hosting.AppHost`,
  `MessagePack` advisories). `dotnet build InvoiceBuilder.slnx` and
  `dotnet test InvoiceBuilder.slnx` are both clean on `master`.

## Milestones

- [x] EF Core + PostgreSQL integration
- [x] Customers CRUD API + cursor pagination
- [x] Senders CRUD API
- [x] Invoices CRUD API (MediatR, validation, `Result<T>`)
- [x] MudBlazor integration, Customers list page
- [x] Customer create/edit/delete UI (see `plans/customer-crud-ui.md`)
- [x] Customers list Previous/Next paging
- [x] API integration test project + Customers endpoint tests
- [x] Merge vulnerable-package fix (`fix/vulnerable-packages`) — a *new* unrelated
      Aspire/MessagePack audit failure now blocks `InvoiceBuilder.AppHost`; see
      Current status.
- [x] Senders UI (list + create/edit/delete), incl. Senders API integration tests (see `plans/senders-ui.md`)
- [x] Invoice builder UI (select customer + sender, add line items, live totals) — create only, no edit yet
- [x] Invoice list + detail view
- [x] Invoice PDF export
- [x] Invoices API integration tests
- [ ] More tests: Application handler tests
- [ ] Authentication / authorization
- [ ] Deployment pipeline

## Open questions

- Multi-tenancy: is a user account tied to one Sender, or many?
- Tax handling: single `TaxRate` per invoice vs. per line item?
- Invoice numbering: user-supplied vs. auto-generated sequence?

## Known issues / follow-ups

- Customer form: the Email field is validated as required but has no `*` marker.
- Invoice PDF export has no automated test coverage (no integration test hits
  `GET /api/invoices/{id}/pdf`, no test on `InvoicePdfDocument`'s layout
  logic). Worth adding when picking up "Application handler tests" below.
- `UpdateInvoiceDto` has no `LineItems` field — editing an invoice cannot change
  its line items via the API as it stands today. The invoice builder UI only
  supports create for this reason; building an edit UI needs this DTO (and
  `UpdateInvoiceHandler`) extended first.
- `CreateInvoiceEndpoint` always returns `400 BadRequest` on handler failure,
  even for what are really 409 (duplicate `InvoiceNumber`) or 404 (unknown
  `CustomerId`/`SenderId`) cases — unlike Senders/Customers delete, which
  branch on the error type. `UpdateInvoiceEndpoint` has the same issue the
  other way: every handler failure maps to `404 NotFound`, including the
  duplicate-`InvoiceNumber` conflict added below. The builder UI's
  `HandleBadRequest` copes by reading the `ResultError` body either way, but
  the API response codes themselves are inconsistent with the rest of the
  app's conventions.
- ~~`UpdateInvoiceHandler` 500s if `CustomerId`/`SenderId` change~~ — fixed:
  the handler now reloads the `Customer`/`Sender` navigation after
  `SaveChangesAsync` instead of relying on the stale pre-update `Include`.
  ~~`UpdateInvoiceDto.InvoiceNumber` was silently ignored~~ — fixed:
  `Invoice.Update(...)` now takes and applies `invoiceNumber`, and the
  handler rejects a change to a number already used by another invoice
  (`InvoiceAlreadyExists`, mirroring Create). Both found and fixed via the
  Invoices integration tests (`InvoicesEndpointsTests.cs`).
- Sender/Customer lists are ordered by a random `Guid` `Id`, not by name, so row
  order is arbitrary (stable across pages).
- After creating a customer, the new row is inserted at the top of the loaded
  page, so the page can briefly show more rows than the page size until the
  user navigates (consequence of the in-place list update).
- `NU1903` for `SSH.NET` 2024.2.0 via `Testcontainers` 4.6.0 — test project only,
  non-fatal; resolve with a newer Testcontainers or a direct `SSH.NET` pin.
- `Components/Layout/NavMenu.razor` is stale Bootstrap markup and unused; the
  live navigation is `MudNavMenu` in `MainLayout.razor`.
- Server-side error paths in the Customers UI (400 mapping, 404 on edit/delete,
  network failure) are implemented but not manually verified.
- The `.claude/` folder (including the `create-feature` skill) and `CLAUDE.md`
  are untracked in git.

## Out of scope (for now)

- Payments / payment-gateway integration
- Recurring invoices and subscriptions
- Multi-currency conversion (currency is stored, not converted)
- Email delivery of invoices
