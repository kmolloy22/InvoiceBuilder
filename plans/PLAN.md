# InvoiceBuilder — Project Plan

_Last updated: 2026-08-31_

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
  - `InvoiceBuilder.Database` — EF Core + PostgreSQL, configurations and
    migrations.
  - `InvoiceBuilder.Shared.Kernel` — cross-cutting primitives.
- **Frontend** (`frontend/src/InvoiceBuilder.Web`):
  - Blazor Server, MudBlazor component library.
  - Refit-based typed API clients in `Services/`.

## Current status

| Area                    | API           | Frontend      |
| ----------------------- | ------------- | ------------- |
| Customers               | Full CRUD     | List page     |
| Senders                 | Full CRUD     | —             |
| Invoices                | Full CRUD     | —             |
| Line items              | Via Invoice   | —             |

- EF migrations: `InitialCreate`, `AddInvoice`.
- No automated tests yet (`backend/tests`, `frontend/tests` are empty).

## Milestones

- [x] EF Core + PostgreSQL integration
- [x] Customers CRUD API + cursor pagination
- [x] Senders CRUD API
- [x] Invoices CRUD API (MediatR, validation, `Result<T>`)
- [x] MudBlazor integration, Customers list page
- [ ] Customer create/edit/delete UI
- [ ] Senders UI (list + create/edit/delete)
- [ ] Invoice builder UI (select customer + sender, add line items, live totals)
- [ ] Invoice list + detail view
- [ ] Invoice PDF export
- [ ] Test projects: Application handler tests, API integration tests
- [ ] Authentication / authorization
- [ ] Deployment pipeline

## Open questions

- Multi-tenancy: is a user account tied to one Sender, or many?
- Tax handling: single `TaxRate` per invoice vs. per line item?
- PDF generation approach: server-side library (QuestPDF) vs. HTML-to-PDF?
- Invoice numbering: user-supplied vs. auto-generated sequence?

## Out of scope (for now)

- Payments / payment-gateway integration
- Recurring invoices and subscriptions
- Multi-currency conversion (currency is stored, not converted)
- Email delivery of invoices
