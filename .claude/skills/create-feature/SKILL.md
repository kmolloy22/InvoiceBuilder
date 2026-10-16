---
name: create-feature
description: Scaffold a new API operation (vertical slice) in the InvoiceBuilder .NET solution, following the Customers/CustomerCreate pattern across Application (MediatR command + handler, FluentValidation, DTOs, Result) and Api (minimal-API endpoint, ValidationFilter, route-group registration), plus integration tests and an optional Refit client method. Use this whenever the user wants to add, create, scaffold, or extend an endpoint, API operation, command, query, or feature in InvoiceBuilder - for Customers, Senders, Invoices, or a new entity - including phrasing like "add a POST/PUT/GET/DELETE for...", "new endpoint for...", "add a duplicate/archive/import operation", or "follow the CustomerCreate pattern", even if they do not say "vertical slice".
---

# create-feature: add an operation to InvoiceBuilder

Every operation in this repo is a vertical slice with the same shape. This skill reproduces that
shape so a new endpoint lands consistent with `Customers/CustomerCreate` and builds cleanly under
the repo's strict analyzers (warnings are errors in product projects).

## 1. Confirm the plan before touching files

The user wants to approve changes to the codebase first, and scaffolding is cheap to redo but
annoying to unpick. Before writing anything, read `plans/PLAN.md` if the feature touches roadmap
scope, then agree these points with the user (propose defaults, do not interrogate):

- **Feature / Entity / Operation**: e.g. `Customers` / `Customer` / `Duplicate` (see naming rules in
  `references/customer-create-walkthrough.md`)
- **HTTP verb and route** under `api/{feature}`
- **Request fields** (name and type) and, for writes, the response shape
- **Failure cases** and the status each should return
- **Persistence impact**: does it need a new entity, column, or migration? Those are separate
  decisions (`references/variants.md`, last section) - ask before making them
- **Scope**: API only, or also the Refit client and Blazor UI, and whether to add tests (default: yes)

Show the list of files you will create or edit, and wait for a yes.

## 2. Build the slice

The slice, in dependency order (all paths under `backend/src/`):

| Layer | File | Template |
|-------|------|----------|
| Domain | `InvoiceBuilder.Domain/Entities/{Entity}.cs` - add a method only if state changes | by hand |
| Application | `Features/{Feature}/Models/{Op}/{Op}{Entity}Dto.cs` | `assets/Dto.cs.tmpl` |
| Application | `Features/{Feature}/Models/{Op}/{Op}{Entity}ResponseDto.cs` (POST only) | `assets/ResponseDto.cs.tmpl` |
| Application | `Shared/Responses/{Feature}/{Op}{Entity}Result.cs` | `assets/Result.cs.tmpl` |
| Application | `Features/{Feature}/Validation/{Op}{Entity}DtoValidator.cs` | `assets/Validator.cs.tmpl` |
| Application | `Features/{Feature}/{Op}{Entity}.Handler.cs` (command + handler) | `assets/Handler.cs.tmpl` |
| Api | `Features/{Feature}/{Entity}{Op}/{Op}{Entity}.Endpoint.cs` | `assets/Endpoint.cs.tmpl` |
| Api | `Features/{Feature}/{Entity}Endpoint.cs` - add `using` + `group.Map{Op}{Entity}Endpoint();` | edit |
| Tests | `backend/tests/InvoiceBuilder.Api.IntegrationTests/{Feature}EndpointsTests.cs` | `assets/EndpointTests.cs.tmpl` |

**For a POST/create-shaped operation**, run the script. It only creates new files, never
overwrites, and `-WhatIf` previews:

```powershell
./.claude/skills/create-feature/scripts/new-slice.ps1 `
    -Feature Customers -Entity Customer -Operation Duplicate `
    -Fields "CompanyName:string,CustomerName:string,CustomerEmail:string" -WhatIf
```

Drop `-WhatIf` to write the files. `-Fields` must list the entity `Create(...)` factory's parameters in
order, because the handler passes them through positionally. The script needs the entity to exist
already and prints the manual steps that remain (registration, summaries, tests).

**For GET / PUT / DELETE / list**, the create templates are the wrong starting point. Copy the closest
existing Customers file and apply the differences in `references/variants.md`.

Then do the parts the script deliberately leaves to you:

1. Register the endpoint in `{Entity}Endpoint.cs`.
2. Replace the endpoint's `TODO` summary/description and set `Produces...` to the status codes it really returns.
3. Add validator rules beyond `NotEmpty()` (the script only guesses for strings and Guids).
4. Map failures to the right HTTP status (see the caveats in `references/variants.md`).
5. Add integration tests from `assets/EndpointTests.cs.tmpl`: happy path, validation 400, and 404 where an id is involved.
6. If asked, add the Refit method to `frontend/src/InvoiceBuilder.Web/Services/I{Feature}ApiClient.cs`.

## 3. Verify

```bash
dotnet build InvoiceBuilder.slnx -c Release
dotnet test backend/tests/InvoiceBuilder.Api.IntegrationTests -c Release   # needs Docker running (Testcontainers)
```

A clean build is the real check: `TreatWarningsAsErrors` is on, and SonarAnalyzer rules fail the
build. If Docker is not available, say so rather than claiming the tests passed. After a feature
lands, tick the milestone in `plans/PLAN.md` if there is one.

## Conventions that are easy to get wrong

- **No DI wiring.** MediatR and FluentValidation register by assembly scan, so a new handler or
  validator works as soon as it compiles. Adding registrations is a sign something is off.
- **Handlers return `Result<T>` and never throw for expected failures**; endpoints translate the
  result to HTTP. Do not add try/catch - `GlobalExceptionHandler` covers the unexpected.
- **Validation lives in the FluentValidation validator**, applied by
  `.AddEndpointFilter<ValidationFilter<TDto>>()`. Never re-check DTO fields in the endpoint or handler.
- **Never return EF entities.** Return the Result record from the handler and a response DTO from the endpoint.
- **List endpoints use cursor paging via `CursorPagination.CreatePage`**, not `Skip`/`Take`.
- **Formatting:** tabs, LF, `sealed` types (handler is `internal sealed class`, so it does not add a
  CA1852 warning). The test project is the exception: it uses 4-space indentation and
  `TreatWarningsAsErrors=false`.
- **Do not copy existing quirks** (wrong `Produces(204)` on update, unconstrained `{id}` on delete,
  the `GetInvoicess` typo). They are listed in `references/variants.md`.

## References

- `references/customer-create-walkthrough.md` - the reference slice layer by layer, with the
  naming rules and the behaviour to reproduce. Read this first if the slice is unfamiliar.
- `references/variants.md` - how GET / PUT / DELETE / list differ from create, known
  inconsistencies to avoid, new-persistence decisions, and the Refit follow-up.
