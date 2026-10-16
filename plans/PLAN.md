# InvoiceBuilder — Project Plan

_Last updated: 2026-10-16_

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
| Senders                 | Full CRUD     | —             |
| Invoices                | Full CRUD     | —             |
| Line items              | Via Invoice   | —             |

- EF migrations: `InitialCreate`, `AddInvoice`.
- Tests: 8 API integration tests, Customers endpoints only (POST, GET by id,
  PUT, DELETE: happy paths, validation 400, 404s). No Senders/Invoices tests,
  no Application handler tests, no frontend tests (`frontend/tests` is empty).
- Customers UI manually smoke-tested end to end on 2026-10-16 (create,
  validation errors, edit, delete, paging) — all passed.
- Build: a NuGet audit failure (`System.Security.Cryptography.Xml`/`Pkcs`
  advisories, escalated by `TreatWarningsAsErrors`) broke restore/build for all
  projects. Fix is on branch `fix/vulnerable-packages` (pending PR); until it is
  merged, `master` does not build and CI will fail.

## Milestones

- [x] EF Core + PostgreSQL integration
- [x] Customers CRUD API + cursor pagination
- [x] Senders CRUD API
- [x] Invoices CRUD API (MediatR, validation, `Result<T>`)
- [x] MudBlazor integration, Customers list page
- [x] Customer create/edit/delete UI (see `plans/customer-crud-ui.md`)
- [x] Customers list Previous/Next paging
- [x] API integration test project + Customers endpoint tests
- [ ] Merge vulnerable-package fix (`fix/vulnerable-packages`) and confirm CI is green
- [ ] Senders UI (list + create/edit/delete)
- [ ] Invoice builder UI (select customer + sender, add line items, live totals)
- [ ] Invoice list + detail view
- [ ] Invoice PDF export
- [ ] More tests: Senders/Invoices API integration tests, Application handler tests
- [ ] Authentication / authorization
- [ ] Deployment pipeline

## Open questions

- Multi-tenancy: is a user account tied to one Sender, or many?
- Tax handling: single `TaxRate` per invoice vs. per line item?
- PDF generation approach: server-side library (QuestPDF) vs. HTML-to-PDF?
- Invoice numbering: user-supplied vs. auto-generated sequence?

## Known issues / follow-ups

- Customer form: the Email field is validated as required but has no `*` marker.
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
